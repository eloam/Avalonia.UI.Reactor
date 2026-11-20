using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SkiaSharp;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D12;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.System.Com;

namespace Avalonia.UI.Reactor.Desktop;

/// <summary>
/// Skia sur Direct3D 12, la voie native de Windows : chaîne d'affichage DXGI « flip » sur la fenêtre SDL.
/// Les interfaces COM sont générées par CsWin32 depuis les métadonnées officielles de Windows (NativeMethods.txt).
/// </summary>
[SupportedOSPlatform("windows10.0.10240")]
internal sealed unsafe class D3D12Backend : IGpuBackend
{
    /// <summary>Deux tampons : l'un est affiché pendant que Skia dessine dans l'autre.</summary>
    const int BufferCount = 2;
    const DXGI_FORMAT BufferFormat = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;

    readonly bool _vsync;
    readonly ID3D12Resource*[] _buffers = new ID3D12Resource*[BufferCount];
    readonly ID3D12CommandAllocator*[] _allocators = new ID3D12CommandAllocator*[BufferCount];
    /// <summary>Valeur de la barrière signalée après la dernière image dessinée dans chaque tampon.</summary>
    readonly ulong[] _bufferFenceValues = new ulong[BufferCount];
    IDXGIFactory2* _factory;
    IDXGIAdapter1* _adapter;
    ID3D12Device* _device;
    ID3D12CommandQueue* _queue;
    IDXGISwapChain3* _swapChain;
    ID3D12Fence* _fence;
    ID3D12GraphicsCommandList* _presentTransition;
    ulong _lastFenceValue;
    GRContext? _gr;
    GRBackendRenderTarget? _target;
    SKSurface? _surface;
    int _width, _height, _index;

    public string Name => "Direct3D 12";

    /// <exception cref="COMException">Direct3D 12 n'est pas utilisable sur cette machine (pilote, carte graphique).</exception>
    /// <exception cref="InvalidOperationException">Skia ne peut pas dessiner avec ce périphérique.</exception>
    public D3D12Backend(HWND window, int width, int height, bool vsync)
    {
        _vsync = vsync;
        try
        {
            CreateDevice();
            CreateSwapChain(window, width, height);
            CreatePresentTransition();
            _gr = GRContext.CreateDirect3D(new GRD3DBackendContext
                  {
                      Adapter = (nint)_adapter,
                      Device = (nint)_device,
                      Queue = (nint)_queue,
                  })
                  ?? throw new InvalidOperationException("Skia could not create a Direct3D 12 context.");
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    void CreateDevice()
    {
        IDXGIFactory2* factory;
        PInvoke.CreateDXGIFactory2(0, Com.IidOf<IDXGIFactory2>(), (void**)&factory).ThrowOnFailure();
        _factory = factory;

        IDXGIAdapter1* adapter;
        _factory->EnumAdapters1(0, &adapter).ThrowOnFailure(); // carte graphique principale
        _adapter = adapter;

        ID3D12Device* device;
        PInvoke.D3D12CreateDevice((IUnknown*)_adapter, D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
            Com.IidOf<ID3D12Device>(), (void**)&device).ThrowOnFailure();
        _device = device;

        var queueDesc = new D3D12_COMMAND_QUEUE_DESC { Type = D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT };
        ID3D12CommandQueue* queue;
        _device->CreateCommandQueue(&queueDesc, Com.IidOf<ID3D12CommandQueue>(), (void**)&queue);
        _queue = queue;

        ID3D12Fence* fence;
        _device->CreateFence(0, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, Com.IidOf<ID3D12Fence>(), (void**)&fence);
        _fence = fence;
    }

    void CreateSwapChain(HWND window, int width, int height)
    {
        var desc = new DXGI_SWAP_CHAIN_DESC1
        {
            Width = (uint)width,
            Height = (uint)height,
            Format = BufferFormat,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1 },
            BufferUsage = DXGI_USAGE.DXGI_USAGE_RENDER_TARGET_OUTPUT,
            BufferCount = BufferCount,
            Scaling = DXGI_SCALING.DXGI_SCALING_STRETCH,
            SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_DISCARD,
        };
        IDXGISwapChain1* swapChain1;
        _factory->CreateSwapChainForHwnd((IUnknown*)_queue, window, &desc, null, null, &swapChain1);
        try
        {
            IDXGISwapChain3* swapChain3;
            ((IUnknown*)swapChain1)->QueryInterface(Com.IidOf<IDXGISwapChain3>(), (void**)&swapChain3).ThrowOnFailure();
            _swapChain = swapChain3;
        }
        finally
        {
            Com.Release(ref swapChain1);
        }
        // Alt+Entrée reste à l'application : pas de bascule plein écran imposée par DXGI.
        _factory->MakeWindowAssociation(window, DXGI_MWA_FLAGS.DXGI_MWA_NO_ALT_ENTER);
        (_width, _height) = (width, height);
        AcquireBuffers();
    }

    /// <summary>
    /// Liste de commandes qui rend le tampon « présentable » après le dessin de Skia.
    /// Contournement : SkiaSharp n'expose pas le vidage « pour présentation » de Skia (accès <c>kPresent</c>),
    /// qui ferait cette transition lui-même ; à retirer quand SkiaSharp l'exposera.
    /// </summary>
    void CreatePresentTransition()
    {
        for (int i = 0; i < BufferCount; i++)
        {
            ID3D12CommandAllocator* allocator;
            _device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT,
                Com.IidOf<ID3D12CommandAllocator>(), (void**)&allocator);
            _allocators[i] = allocator;
        }
        ID3D12GraphicsCommandList* list;
        _device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT, _allocators[0], null,
            Com.IidOf<ID3D12GraphicsCommandList>(), (void**)&list);
        _presentTransition = list;
        _presentTransition->Close();
    }

    void AcquireBuffers()
    {
        for (uint i = 0; i < BufferCount; i++)
        {
            ID3D12Resource* buffer;
            _swapChain->GetBuffer(i, Com.IidOf<ID3D12Resource>(), (void**)&buffer);
            _buffers[i] = buffer;
        }
    }

    void ReleaseBuffers()
    {
        for (int i = 0; i < BufferCount; i++) Com.Release(ref _buffers[i]);
    }

    public SKSurface? BeginFrame(int width, int height)
    {
        if (width != _width || height != _height) Resize(width, height);
        _index = (int)_swapChain->GetCurrentBackBufferIndex();
        // Enveloppe neuve à chaque image : le tampon revient de l'affichage dans l'état « présentable ».
        _target = new GRBackendRenderTarget(width, height, new GRD3DTextureResourceInfo
        {
            Resource = (nint)_buffers[_index],
            ResourceState = (uint)D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PRESENT,
            Format = (uint)BufferFormat,
            SampleCount = 1,
            LevelCount = 1,
        });
        _surface = SKSurface.Create(_gr, _target, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888);
        return _surface;
    }

    public bool EndFrame()
    {
        _surface?.Flush(submit: true);
        TransitionToPresent();
        var result = _swapChain->Present(_vsync ? 1u : 0u, 0);
        _bufferFenceValues[_index] = Signal();
        _surface?.Dispose();
        _target?.Dispose();
        _surface = null;
        _target = null;
        if (result == HRESULT.DXGI_ERROR_DEVICE_REMOVED || result == HRESULT.DXGI_ERROR_DEVICE_RESET) return false;
        result.ThrowOnFailure();
        return true;
    }

    /// <summary>Le tampon courant passe de « cible de rendu » (état laissé par Skia) à « présentable ».</summary>
    void TransitionToPresent()
    {
        var allocator = _allocators[_index];
        WaitForFence(_bufferFenceValues[_index]); // le GPU a fini d'utiliser cet allocateur
        allocator->Reset();
        _presentTransition->Reset(allocator, null);
        var barrier = new D3D12_RESOURCE_BARRIER { Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION };
        barrier.Transition.pResource = _buffers[_index];
        barrier.Transition.Subresource = PInvoke.D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
        barrier.Transition.StateBefore = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET;
        barrier.Transition.StateAfter = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PRESENT;
        _presentTransition->ResourceBarrier(1, &barrier);
        _presentTransition->Close();
        var list = (ID3D12CommandList*)_presentTransition;
        _queue->ExecuteCommandLists(1, &list);
    }

    /// <summary>Nouvelle taille : attendre le GPU, lâcher les tampons, les redimensionner, les reprendre.</summary>
    void Resize(int width, int height)
    {
        WaitForGpu();
        ReleaseBuffers();
        _gr?.PurgeUnlockedResources(scratchResourcesOnly: false); // Skia ne doit plus retenir les anciens tampons
        _swapChain->ResizeBuffers(0 /* même nombre */, (uint)width, (uint)height, DXGI_FORMAT.DXGI_FORMAT_UNKNOWN /* même format */, 0);
        (_width, _height) = (width, height);
        AcquireBuffers();
    }

    ulong Signal()
    {
        _queue->Signal(_fence, ++_lastFenceValue);
        return _lastFenceValue;
    }

    /// <summary>Attend que le GPU ait atteint <paramref name="value"/> (sans événement, l'appel bloque jusque-là).</summary>
    void WaitForFence(ulong value)
    {
        if (value == 0 || _fence->GetCompletedValue() >= value) return;
        _fence->SetEventOnCompletion(value, default(HANDLE)); // sans événement : bloque jusqu'à la valeur
    }

    void WaitForGpu()
    {
        if (_gr is not null)
        {
            _gr.Flush();
            _gr.Submit(synchronous: true);
        }
        if (_queue is not null && _fence is not null) WaitForFence(Signal());
    }

    public void Dispose()
    {
        try
        {
            WaitForGpu();
        }
        catch (COMException)
        {
            // GPU perdu : rien à attendre, on libère quand même.
        }
        _surface?.Dispose();
        _target?.Dispose();
        _gr?.Dispose();
        _gr = null;
        ReleaseBuffers();
        for (int i = 0; i < BufferCount; i++) Com.Release(ref _allocators[i]);
        Com.Release(ref _presentTransition);
        Com.Release(ref _fence);
        Com.Release(ref _swapChain);
        Com.Release(ref _queue);
        Com.Release(ref _device);
        Com.Release(ref _adapter);
        Com.Release(ref _factory);
    }
}
