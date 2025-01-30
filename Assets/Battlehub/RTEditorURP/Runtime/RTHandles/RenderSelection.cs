using Battlehub.RTCommon;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNIVERSAL_RP_17_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace Battlehub.RTHandles.URP
{
    public class RenderSelection : ScriptableRendererFeature
    {
        [System.Serializable]
        public class RenderSelectionSettings
        {
            public RenderPassEvent RenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            public LayerMask LayerMask = -1;

            public Color OutlineColor = new Color32(255, 128, 0, 255);

            [Range(0.5f, 10f)]
            public float OutlineStength = 5;

            [Range(0.1f, 3)]
            public float BlurStrength = 1f;

            public string MeshesCacheName = "SelectedMeshes";
            public string RenderersCacheName = "SelectedRenderers";
            public string CustomRenderersCacheName = "CustomOutlineRenderersCache";

            [HideInInspector, NonSerialized]
            public Material MaskMaterial = null;

            [HideInInspector, NonSerialized]
            public Material BlurMaterial = null;

            [HideInInspector, NonSerialized]
            public Material CompositeMaterial = null;

            public float BlurSize
            {
                get { return BlurStrength; }
                set { BlurStrength = value; }
            }
        }

        [SerializeField]
        public RenderSelectionSettings m_settings = new RenderSelectionSettings();

        private void Reset()
        {
            m_settings.LayerMask =
                1 << CameraLayerSettings.Default.AllScenesLayer |
                1 << CameraLayerSettings.Default.RuntimeGraphicsLayer |
                1 << (CameraLayerSettings.Default.RuntimeGraphicsLayer + 1) |
                1 << (CameraLayerSettings.Default.RuntimeGraphicsLayer + 2) |
                1 << (CameraLayerSettings.Default.RuntimeGraphicsLayer + 3);
        }

        private class RenderSelectionPass : ScriptableRenderPass
        {
            public RenderSelectionSettings Settings;

            private IMeshesCache m_meshesCache;
            private IRenderersCache m_renderersCache;
            private ICustomOutlineRenderersCache m_customRenderersCache;

            private RenderTargetIdentifier m_prepassRT;
            private RTHandle m_prepassHandle;
            private RenderTargetIdentifier m_blurredRT;
            private RenderTargetIdentifier m_tmpRT;
            private int m_tmpTexId;
            private int m_prepassId;
            private int m_blurredId;
            private int m_blurDirectionId;

#if UNIVERSAL_RP_15_0_OR_NEWER
            private RTHandle m_cameraColorRT;
#else
            private RenderTargetIdentifier m_cameraColorRT;
#endif
            private int m_outlineColorId;
            private int m_outlineStrengthId;
            private int m_blurStrengthId;
#if UNIVERSAL_RP_17_0_OR_NEWER
            
            private int m_maskTextureId;
            private int m_blurTextureId;
            private RenderTextureDescriptor m_maskTexture;
            private RenderTextureDescriptor m_blurVertTexture;
            private RenderTextureDescriptor m_blurTexture;
            private RenderTextureDescriptor m_camTexture;
            
            public RenderSelectionPass()
            {
                m_maskTexture = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
                m_blurVertTexture = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
                m_blurTexture = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
                m_camTexture = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
                m_maskTextureId = Shader.PropertyToID("_MaskTex");
                m_blurTextureId = Shader.PropertyToID("_BlurTex");
                m_outlineColorId = Shader.PropertyToID("_OutlineColor");
                m_outlineStrengthId = Shader.PropertyToID("_OutlineStrength");
                m_blurStrengthId = Shader.PropertyToID("_BlurStrength");
            }

            public void Setup(RenderSelectionSettings settings, IMeshesCache meshesCache, IRenderersCache renderersCache, ICustomOutlineRenderersCache customRenderersCache)
            {
                Settings = settings;
                Settings.CompositeMaterial.SetColor(m_outlineColorId, Settings.OutlineColor);
                Settings.BlurMaterial.SetFloat(m_outlineStrengthId, Settings.OutlineStength);
                Settings.BlurMaterial.SetFloat(m_blurStrengthId, Settings.BlurStrength);

                m_meshesCache = meshesCache;
                m_renderersCache = renderersCache;
                m_customRenderersCache = customRenderersCache;
            }

            private class MaskPassData
            {
                public IMeshesCache MeshesCache;
                public IRenderersCache RenderersCache;
                public ICustomOutlineRenderersCache CustomRenderersCache;
                public Material Material;
            }

            private static void ExecuteMaskPass(MaskPassData data, RasterGraphContext context)
            {
                var cmd = context.cmd;
                cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 1));

                if (data.MeshesCache != null)
                {
                    IList<RenderMeshesBatch> batches = data.MeshesCache.Batches;
                    for (int i = 0; i < batches.Count; ++i)
                    {
                        RenderMeshesBatch batch = batches[i];
                        for (int j = 0; j < batch.Mesh.subMeshCount; ++j)
                        {
                            if (batch.Mesh != null)
                            {
                                cmd.DrawMeshInstanced(batch.Mesh, j, data.Material, 0, batch.Matrices, batch.Matrices.Length);
                            }
                        }
                    }
                }

                if (data.RenderersCache != null)
                {
                    IList<Renderer> renderers = data.RenderersCache.Renderers;
                    for (int i = 0; i < renderers.Count; ++i)
                    {
                        Renderer renderer = renderers[i];
                        if (renderer != null && renderer.enabled && renderer.gameObject.activeSelf)
                        {
                            Material[] materials = renderer.sharedMaterials;

                            for (int j = 0; j < materials.Length; ++j)
                            {
                                if (materials[j] != null)
                                {
                                    cmd.DrawRenderer(renderer, data.Material, j);
                                }
                            }
                        }
                    }
                }

                if (data.CustomRenderersCache != null)
                {
                    List<ICustomOutlinePrepass> renderers = data.CustomRenderersCache.GetOutlineRendererItems();
                    for (int i = 0; i < renderers.Count; ++i)
                    {
                        ICustomOutlinePrepass renderer = renderers[i];
                        if (renderer != null && renderer.GetRenderer().gameObject.activeSelf)
                        {
                            Material[] materials = renderer.GetRenderer().sharedMaterials;

                            for (int j = 0; j < materials.Length; ++j)
                            {
                                if (materials[j] != null)
                                {
                                    cmd.DrawRenderer(renderer.GetRenderer(), renderer.GetOutlinePrepassMaterial(), j);
                                }
                            }
                        }
                    }
                }
            }

            private class PassData
            {
                public TextureHandle SrcTexture;
                public Material Material;
            }

            private static void ExecutePass(PassData data, RasterGraphContext context, int pass)
            {
                Blitter.BlitTexture(context.cmd, data.SrcTexture, new Vector4(1f, 1f, 0f, 0f), data.Material, pass);
            }

            private static void ExecutePass(PassData data, RasterGraphContext context)
            {
                Blitter.BlitTexture(context.cmd, data.SrcTexture, new Vector4(1f, 1f, 0f, 0f), 0, false);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();

                // The following line ensures that the render pass doesn't blit
                // from the back buffer.
                if (resourceData.isActiveTargetBackBuffer)
                    return;

                m_maskTexture.width = cameraData.cameraTargetDescriptor.width;
                m_maskTexture.height = cameraData.cameraTargetDescriptor.height;
                m_maskTexture.depthBufferBits = 0;

                m_blurVertTexture.width = cameraData.cameraTargetDescriptor.width;
                m_blurVertTexture.height = cameraData.cameraTargetDescriptor.height;
                m_blurVertTexture.depthBufferBits = 0;

                m_blurTexture.width = cameraData.cameraTargetDescriptor.width;
                m_blurTexture.height = cameraData.cameraTargetDescriptor.height;
                m_blurTexture.depthBufferBits = 0;

                m_camTexture.width = cameraData.cameraTargetDescriptor.width;
                m_camTexture.height = cameraData.cameraTargetDescriptor.height;
                m_camTexture.depthBufferBits = 0;

                var srcCamColor = resourceData.activeColorTexture;
                var maskTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, m_maskTexture, "_MaskTexture", false);
                var blurVertTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, m_blurVertTexture, "_BlurVertTexture", false);
                var blurTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, m_blurTexture, "_BlurTexture", false);
                var camTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, m_camTexture, "_CamTexture", false);

                // This check is to avoid an error from the material preview in the scene
                if (!srcCamColor.IsValid() || !maskTexture.IsValid() || !blurVertTexture.IsValid() || !blurTexture.IsValid() || !camTexture.IsValid())
                    return;

                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("RenderSelection Mask", out var passData))
                {
                    passData.RenderersCache = m_renderersCache;
                    passData.MeshesCache = m_meshesCache;
                    passData.CustomRenderersCache = m_customRenderersCache;
                    passData.Material = Settings.MaskMaterial;

                    builder.SetRenderAttachment(maskTexture, 0);
                    builder.SetGlobalTextureAfterPass(maskTexture, m_maskTextureId);

                    builder.SetRenderFunc((MaskPassData data, RasterGraphContext context) => ExecuteMaskPass(data, context));
                }

                // Vertical blur pass
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("RenderSelection VBlur", out var passData))
                {
                    passData.SrcTexture = maskTexture;
                    passData.Material = Settings.BlurMaterial;

                    builder.UseTexture(passData.SrcTexture);
                    builder.SetRenderAttachment(blurVertTexture, 0);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context, 0));
                }

                // Horizontal blur pass
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("RenderSelection HBlur", out var passData))
                {
                    passData.SrcTexture = blurVertTexture;
                    passData.Material = Settings.BlurMaterial;

                    builder.UseTexture(passData.SrcTexture);
                    builder.SetRenderAttachment(blurTexture, 0);
                    builder.SetGlobalTextureAfterPass(blurTexture, m_blurTextureId);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context, 1));
                }

                // Read srcCamera pass
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("RenderSelection SrcCam", out var passData))
                {
                    passData.SrcTexture = srcCamColor;
                    passData.Material = null;

                    builder.UseTexture(passData.SrcTexture);
                    builder.SetRenderAttachment(camTexture, 0);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
                }

                // Composite pass
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("RenderSelection Composite", out var passData))
                {
                    passData.SrcTexture = camTexture;
                    passData.Material = Settings.CompositeMaterial;

                    builder.UseGlobalTexture(m_maskTextureId);
                    builder.UseGlobalTexture(m_blurTextureId);
                    builder.UseTexture(passData.SrcTexture);
                    builder.SetRenderAttachment(srcCamColor, 0);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context, 0));
                }
            }
#endif

#if UNIVERSAL_RP_15_0_OR_NEWER
            public void Setup(RTHandle camerColorRT, IMeshesCache meshesCache, IRenderersCache renderersCache, ICustomOutlineRenderersCache customRenderersCache)
            {
                m_meshesCache = meshesCache;
                m_renderersCache = renderersCache;
                m_customRenderersCache = customRenderersCache;
                m_cameraColorRT = camerColorRT;
            }
#else
            public void Setup(RenderTargetIdentifier camerColorRT, IMeshesCache meshesCache, IRenderersCache renderersCache, ICustomOutlineRenderersCache customRenderersCache)
            {
                m_meshesCache = meshesCache;
                m_renderersCache = renderersCache;
                m_customRenderersCache = customRenderersCache;
                m_cameraColorRT = camerColorRT;
            }
#endif

            private RenderTextureDescriptor GetStereoCompatibleDescriptor(RenderTextureDescriptor descriptor, int width, int height, GraphicsFormat format, int depthBufferBits = 0)
            {
                // Inherit the VR setup from the camera descriptor
                var desc = descriptor;
                desc.depthBufferBits = depthBufferBits;
                desc.msaaSamples = 1;
                desc.width = width;
                desc.height = height;
                desc.graphicsFormat = format;
                return desc;
            }

            #pragma warning disable CS0618 // Type or member is obsolete
            #pragma warning disable CS0672 // Member overrides obsolete member
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor camDesc)
            {
                base.Configure(cmd, camDesc);
#if UNIVERSAL_RP_15_0_OR_NEWER
                if (m_cameraColorRT == null)
                {
                    return;
                }
#endif
                var width = camDesc.width;
                var height = camDesc.height;

                m_prepassId = Shader.PropertyToID("_PrepassTex");
                m_blurredId = Shader.PropertyToID("_BlurredTex");
                m_tmpTexId = Shader.PropertyToID("_TmpTex");
                m_outlineColorId = Shader.PropertyToID("_OutlineColor");
                m_outlineStrengthId = Shader.PropertyToID("_OutlineStrength");
                m_blurDirectionId = Shader.PropertyToID("_BlurDirection");

                var desc = GetStereoCompatibleDescriptor(camDesc, width, height, camDesc.graphicsFormat);
                cmd.GetTemporaryRT(m_prepassId, desc);
                cmd.GetTemporaryRT(m_blurredId, desc);
                cmd.GetTemporaryRT(m_tmpTexId, desc);

                m_prepassRT = new RenderTargetIdentifier(m_prepassId);
                m_blurredRT = new RenderTargetIdentifier(m_blurredId);
                m_tmpRT = new RenderTargetIdentifier(m_tmpTexId);

                m_prepassHandle = UnityEngine.Rendering.RTHandles.Alloc(m_prepassRT);
#if UNIVERSAL_RP_15_0_OR_NEWER
                ConfigureTarget(m_prepassHandle);
#else
                ConfigureTarget(m_prepassRT);
#endif
                ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 1)); 
            }
            #pragma warning restore CS0618 // Type or member is obsolete
            #pragma warning restore CS0672 // Member overrides obsolete member

            #pragma warning disable CS0672 // Member overrides obsolete member
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            #pragma warning restore CS0672 // Member overrides obsolete member
            {
                CommandBuffer cmd = CommandBufferPool.Get("RenderSelection");
                bool draw = false;

                if (m_meshesCache != null)
                {
                    IList<RenderMeshesBatch> batches = m_meshesCache.Batches;
                    for (int i = 0; i < batches.Count; ++i)
                    {
                        RenderMeshesBatch batch = batches[i];
                        for (int j = 0; j < batch.Mesh.subMeshCount; ++j)
                        {
                            if (batch.Mesh != null)
                            {
                                cmd.DrawMeshInstanced(batch.Mesh, j, Settings.MaskMaterial, 0, batch.Matrices, batch.Matrices.Length);
                                draw = true;
                            }
                        }
                    }
                }

                if (m_renderersCache != null)
                {
                    IList<Renderer> renderers = m_renderersCache.Renderers;
                    for (int i = 0; i < renderers.Count; ++i)
                    {
                        Renderer renderer = renderers[i];
                        if (renderer != null && renderer.enabled && renderer.gameObject.activeSelf)
                        {
                            Material[] materials = renderer.sharedMaterials;

                            for (int j = 0; j < materials.Length; ++j)
                            {
                                if (materials[j] != null)
                                {
                                    cmd.DrawRenderer(renderer, Settings.MaskMaterial, j);
                                    draw = true;
                                }
                            }
                        }
                    }
                }

                if (m_customRenderersCache != null)
                {
                    List<ICustomOutlinePrepass> renderers = m_customRenderersCache.GetOutlineRendererItems();
                    for (int i = 0; i < renderers.Count; ++i)
                    {
                        ICustomOutlinePrepass renderer = renderers[i];
                        if (renderer != null && renderer.GetRenderer().gameObject.activeSelf)
                        {
                            Material[] materials = renderer.GetRenderer().sharedMaterials;

                            for (int j = 0; j < materials.Length; ++j)
                            {
                                if (materials[j] != null)
                                {
                                    cmd.DrawRenderer(renderer.GetRenderer(), renderer.GetOutlinePrepassMaterial(), j);
                                    draw = true;
                                }
                            }
                        }
                    }
                }

                if (draw)
                {
                    cmd.Blit(m_prepassRT, m_blurredRT);
                    cmd.SetGlobalFloat(m_outlineStrengthId, Settings.OutlineStength);
                    cmd.SetGlobalVector(m_blurDirectionId, new Vector2(Settings.BlurStrength, 0));
                    cmd.Blit(m_blurredRT, m_tmpRT, Settings.BlurMaterial, 0);
                    cmd.SetGlobalVector(m_blurDirectionId, new Vector2(0, Settings.BlurStrength));
                    cmd.Blit(m_tmpRT, m_blurredRT, Settings.BlurMaterial, 0);

                    cmd.Blit(m_cameraColorRT, m_tmpRT);
                    cmd.SetGlobalTexture(m_prepassId, m_prepassRT);
                    cmd.SetGlobalTexture(m_blurredId, m_blurredId);
                    cmd.SetGlobalColor(m_outlineColorId, Settings.OutlineColor);
                    cmd.Blit(m_tmpRT, m_cameraColorRT, Settings.CompositeMaterial);

                    context.ExecuteCommandBuffer(cmd);
                }

                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(m_prepassId);
                cmd.ReleaseTemporaryRT(m_blurredId);
                cmd.ReleaseTemporaryRT(m_tmpTexId);
                if (m_prepassHandle != null)
                {
                    m_prepassHandle.Release();
                }
            }
        }

        private RenderSelectionPass m_scriptablePass;
        private bool m_useRenderGraph;
        public override void Create()
        {
            m_scriptablePass = new RenderSelectionPass();
            m_scriptablePass.Settings = m_settings;
            m_scriptablePass.renderPassEvent = m_settings.RenderPassEvent;

#if UNIVERSAL_RP_17_0_OR_NEWER
            var renderGraphSettings = GraphicsSettings.GetRenderPipelineSettings<UnityEngine.Rendering.Universal.RenderGraphSettings>();
            m_useRenderGraph = !renderGraphSettings.enableRenderCompatibilityMode;
            if (m_useRenderGraph)
            {
                m_settings.MaskMaterial = new Material(Shader.Find("Battlehub/URP17/OutlineMask"));
                m_settings.BlurMaterial = new Material(Shader.Find("Battlehub/URP17/OutlineBlur"));
                m_settings.CompositeMaterial = new Material(Shader.Find("Battlehub/URP17/OutlineComposite"));
            }
            else
#endif
            {
                m_settings.MaskMaterial = new Material(Shader.Find("Battlehub/URP/OutlinePrepass"));
                m_settings.BlurMaterial = new Material(Shader.Find("Battlehub/URP/OutlineBlur"));
                m_settings.CompositeMaterial = new Material(Shader.Find("Battlehub/URP/OutlineComposite"));
            }      
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (Application.isPlaying)
            {
                if (m_settings.MaskMaterial != null)
                {
                    Destroy(m_settings.MaskMaterial);
                }

                if (m_settings.BlurMaterial != null)
                {
                    Destroy(m_settings.BlurMaterial);
                }

                if (m_settings.CompositeMaterial != null)
                {
                    Destroy(m_settings.CompositeMaterial);
                }
            }
            else
            {
                if (m_settings.MaskMaterial != null)
                {
                    DestroyImmediate(m_settings.MaskMaterial);
                }

                if (m_settings.BlurMaterial != null)
                {
                    DestroyImmediate(m_settings.BlurMaterial);
                }

                if (m_settings.CompositeMaterial != null)
                {
                    DestroyImmediate(m_settings.CompositeMaterial);
                }
            }
        }

#if UNIVERSAL_RP_13_1_OR_NEWER
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            if ((renderingData.cameraData.camera.cullingMask & m_settings.LayerMask) != 0)
            {
                var meshesCache = IOC.Resolve<IMeshesCache>(m_settings.MeshesCacheName);
                var renderersCache = IOC.Resolve<IRenderersCache>(m_settings.RenderersCacheName);
                var customRenderersCache = IOC.Resolve<ICustomOutlineRenderersCache>(m_settings.CustomRenderersCacheName);

                if ((meshesCache == null || meshesCache.IsEmpty) && (renderersCache == null || renderersCache.IsEmpty) && (customRenderersCache == null || customRenderersCache.GetOutlineRendererItems().Count == 0))
                {
                    return;
                }

#if UNIVERSAL_RP_17_0_OR_NEWER
                if (m_useRenderGraph)
                {
                    m_scriptablePass.Setup(m_settings, meshesCache, renderersCache, customRenderersCache);
                }
#endif
                renderer.EnqueuePass(m_scriptablePass);
            }
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if ((renderingData.cameraData.camera.cullingMask & m_settings.LayerMask) != 0)
            {
                IMeshesCache meshesCache = IOC.Resolve<IMeshesCache>(m_settings.MeshesCacheName);
                IRenderersCache renderersCache = IOC.Resolve<IRenderersCache>(m_settings.RenderersCacheName);
                ICustomOutlineRenderersCache customRenderersCache = IOC.Resolve<ICustomOutlineRenderersCache>(m_settings.CustomRenderersCacheName);

                if ((meshesCache == null || meshesCache.IsEmpty) && (renderersCache == null || renderersCache.IsEmpty) && (customRenderersCache == null || customRenderersCache.GetOutlineRendererItems().Count == 0))
                {
                    return;
                }


                if (!m_useRenderGraph)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var src = renderer.cameraColorTargetHandle;
#pragma warning restore CS0618 // Type or member is obsolete
                    m_scriptablePass.Setup(src, meshesCache, renderersCache, customRenderersCache);
                }
            }
        }

#else
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if ((renderingData.cameraData.camera.cullingMask & m_settings.LayerMask) != 0)
            {
                IMeshesCache meshesCache = IOC.Resolve<IMeshesCache>(m_settings.MeshesCacheName);
                IRenderersCache renderersCache = IOC.Resolve<IRenderersCache>(m_settings.RenderersCacheName);
                ICustomOutlineRenderersCache customRenderersCache = IOC.Resolve<ICustomOutlineRenderersCache>(m_settings.CustomRenderersCacheName);

                if ((meshesCache == null || meshesCache.IsEmpty) && (renderersCache == null || renderersCache.IsEmpty) && (customRenderersCache == null || customRenderersCache.GetOutlineRendererItems().Count == 0))
                {
                    return;
                }

                var src = renderer.cameraColorTarget;
                m_scriptablePass.Setup(src, meshesCache, renderersCache, customRenderersCache);
                renderer.EnqueuePass(m_scriptablePass);
            }
        }
#endif
    }
}
