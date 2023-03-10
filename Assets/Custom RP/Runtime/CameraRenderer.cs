using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    private ScriptableRenderContext _context;
    private Camera camera;
    private const string bufferName = "Render Camera";
    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    private CullingResults _cullingResults;


    public void Render(ScriptableRenderContext context, Camera camera)
    {
        _context = context;
        this.camera = camera;

        PrepareBuffer();
        PrepareForSceneView();
        if (!Cull())
            return;

        SetUp();
        DrawVisibleGeometry();
        DrawUnsupportedShaders();
        DrawGizmos();
        Submit();
    }

    bool Cull()
    {
        if (camera.TryGetCullingParameters(out var p))
        {
            _cullingResults = _context.Cull(ref p);
            return true;
        }

        return false;
    }

    void SetUp()
    {
        _context.SetupCameraProperties(camera);
        buffer.ClearRenderTarget(true, true, Color.clear);
        buffer.BeginSample(SampleName);
        ExcuteBuffer();
    }

    void DrawVisibleGeometry()
    {
        var sortingSetting = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawSettings = new DrawingSettings(unlitShaderTagId, sortingSetting);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        _context.DrawRenderers(_cullingResults, ref drawSettings, ref filteringSettings);
        _context.DrawSkybox(camera);

        sortingSetting.criteria = SortingCriteria.CommonTransparent;
        drawSettings.sortingSettings = sortingSetting;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        _context.DrawRenderers(_cullingResults, ref drawSettings, ref filteringSettings);
    }

    void Submit()
    {
        buffer.EndSample(SampleName);
        ExcuteBuffer();
        _context.Submit();
    }

    void ExcuteBuffer()
    {
        _context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

}