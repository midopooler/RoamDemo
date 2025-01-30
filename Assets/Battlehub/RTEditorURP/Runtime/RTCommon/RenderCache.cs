using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using UnityEngine.Rendering.Universal;

#if UNIVERSAL_RP_17_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace Battlehub.RTCommon.URP
{
    public class RenderCache : ScriptableRendererFeature
    {
        [Serializable]
        public class RenderCacheSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;
            public string RenderersCacheName = "RenderersCache";
        }

        [SerializeField]
        public RenderCacheSettings m_settings = new RenderCacheSettings();

        private class RenderCachePass : ScriptableRenderPass
        {
            private IRenderersCache m_renderersCache;
            public void Setup(IRenderersCache renderersCache)
            {
                m_renderersCache = renderersCache;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("RenderCache");

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
                                    cmd.DrawRenderer(renderer, materials[j], j);
                                }
                            }
                        }
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            #if UNIVERSAL_RP_17_0_OR_NEWER
            private class PassData
            {
                public IRenderersCache RenderersCache;
            }

            private static void ExecutePass(PassData data, RasterGraphContext context)
            {
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
                                    context.cmd.DrawRenderer(renderer, materials[j], j);
                                }
                            }
                        }
                    }
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Render Cache", out var passData))
                {
                    UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();

                    passData.RenderersCache = m_renderersCache;

                    UniversalResourceData frameData = frameContext.Get<UniversalResourceData>();
                    builder.SetRenderAttachment(frameData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(frameData.activeDepthTexture, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
                }
            }
            #endif
        }

        private RenderCachePass m_ScriptablePass;

        public override void Create()
        {
            m_ScriptablePass = new RenderCachePass();
            m_ScriptablePass.renderPassEvent = m_settings.Event;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            IRenderersCache renderersCache = IOC.Resolve<IRenderersCache>(m_settings.RenderersCacheName);
            if (renderersCache == null || renderersCache.IsEmpty)
            {
                return;
            }

            m_ScriptablePass.Setup(renderersCache);
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}
