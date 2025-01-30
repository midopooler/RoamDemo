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
    public class RenderGizmos : ScriptableRendererFeature
    {
        private Dictionary<RenderPassEvent, Dictionary<Camera, List<IRTECamera>>> m_eventToCameras;
        private RendererPass[] m_scriptablePasses;

        public override void Create()
        {
            m_eventToCameras = new Dictionary<RenderPassEvent, Dictionary<Camera, List<IRTECamera>>>();

            RTECamera[] cameras = UnityObjectExt.FindObjectsByType<RTECamera>();
            foreach (RTECamera camera in cameras)
            {
                AddCamera(camera);
            }

            RTECamera.Created += OnRTECameraCreated;
            RTECamera.Destroyed += OnRTECameraDestroyed;

            m_scriptablePasses = new[]
            {
                CreatePass(CameraEvent.AfterForwardAlpha),
                CreatePass(CameraEvent.BeforeImageEffects),
                CreatePass(CameraEvent.AfterImageEffectsOpaque),
                CreatePass(CameraEvent.AfterImageEffects),
            };
        }
        private RendererPass CreatePass(CameraEvent camEvent)
        {
            return new RendererPass(camEvent, m_eventToCameras) { renderPassEvent = ToRenderPassEvent(camEvent) };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            RTECamera.Created -= OnRTECameraCreated;
            RTECamera.Destroyed -= OnRTECameraDestroyed;

            m_eventToCameras.Clear();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            for (int i = 0; i < m_scriptablePasses.Length; ++i)
            {
                var pass = m_scriptablePasses[i];
                if (m_eventToCameras.TryGetValue(pass.renderPassEvent, out var cameras) && cameras.ContainsKey(renderingData.cameraData.camera))
                {
                    renderer.EnqueuePass(pass);
                }
            }
        }

        private void OnRTECameraCreated(IRTECamera camera)
        {
            AddCamera(camera);
        }

        private void OnRTECameraDestroyed(IRTECamera camera)
        {
            RemoveCamera(camera);
        }

        private void AddCamera(IRTECamera camera)
        {
            var renderPassEvent = ToRenderPassEvent(camera.Event);
            if (!m_eventToCameras.TryGetValue(renderPassEvent, out var cameras))
            {
                cameras = new Dictionary<Camera, List<IRTECamera>>();
                m_eventToCameras.Add(renderPassEvent, cameras);
            }

            if (!cameras.TryGetValue(camera.Camera, out var rteCameraList))
            {
                rteCameraList = new List<IRTECamera>();
                cameras.Add(camera.Camera, rteCameraList);
            }

            rteCameraList.Add(camera);
        }

        private void RemoveCamera(IRTECamera camera)
        {
            var renderPassEvent = ToRenderPassEvent(camera.Event);
            if (!m_eventToCameras.TryGetValue(renderPassEvent, out var cameras))
            {
                return;
            }

            if (!cameras.TryGetValue(camera.Camera, out var rteCameraList))
            {
                return;
            }

            rteCameraList.Remove(camera);
            if (rteCameraList.Count == 0)
            {
                cameras.Remove(camera.Camera);
            }

            if (cameras.Count == 0)
            {
                m_eventToCameras.Remove(renderPassEvent);
            }
        }

        private RenderPassEvent ToRenderPassEvent(CameraEvent cameraEvent)
        {
            switch (cameraEvent)
            {
                case CameraEvent.BeforeImageEffects:
                    return RenderPassEvent.BeforeRenderingPostProcessing;
                case CameraEvent.AfterImageEffects:
                case CameraEvent.AfterImageEffectsOpaque:
                    return RenderPassEvent.AfterRenderingPostProcessing;
                case CameraEvent.AfterForwardAlpha:
                    return RenderPassEvent.AfterRenderingTransparents;
                default:
                    return RenderPassEvent.AfterRendering;
            }
        }

        private class RendererPass : ScriptableRenderPass
        {
            private Dictionary<RenderPassEvent, Dictionary<Camera, List<IRTECamera>>> m_eventToCameras;
            private CameraEvent m_cameraEvent;

            public RendererPass(CameraEvent cameraEvent, Dictionary<RenderPassEvent, Dictionary<Camera, List<IRTECamera>>> eventToCameras)
            {
                m_cameraEvent = cameraEvent;
                m_eventToCameras = eventToCameras;

            }

#pragma warning disable CS0672 // Member overrides obsolete member
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (renderingData.cameraData.camera.HasCmdBuffers())
                {
                    var cmdBuffers = renderingData.cameraData.camera.GetCmdBuffers(m_cameraEvent);
                    for (int i = 0; i < cmdBuffers.Count; ++i)
                    {
                        context.ExecuteCommandBuffer(cmdBuffers[i]);
                    }
                }
            }
#pragma warning restore CS0672 // Member overrides obsolete member
#if UNIVERSAL_RP_17_0_OR_NEWER
            private class PassData
            {
                public IList<IRTECamera> Cameras;
                public bool ClearDepth;
            }


            private class RasterCommandBufferWrapper : IRTECommandBuffer
            {
                public RasterCommandBuffer cmd;

                public void Clear()
                {
                }

                public void ClearRenderTarget(bool clearDepth, bool clearColor, Color backgroundColor)
                {
                    cmd.ClearRenderTarget(clearDepth, clearColor, backgroundColor);
                }

                public void ClearRenderTarget(bool clearDepth, bool clearColor, Color backgroundColor, float depth)
                {
                    cmd.ClearRenderTarget(clearDepth, clearColor, backgroundColor, depth);
                }

                public void ClearRenderTarget(bool clearDepth, bool clearColor, Color backgroundColor, float depth, uint stencil)
                {
                    cmd.ClearRenderTarget(clearDepth, clearColor, backgroundColor, depth, stencil);
                }

                public void ClearRenderTarget(RTClearFlags clearFlags, Color backgroundColor, float depth, uint stencil)
                {
                    cmd.ClearRenderTarget(clearFlags, backgroundColor, depth, stencil);
                }

                public void ClearRenderTarget(RTClearFlags clearFlags, Color[] backgroundColors, float depth, uint stencil)
                {
                    cmd.ClearRenderTarget(clearFlags, backgroundColors, depth, stencil);
                }

                public void ConfigureFoveatedRendering(IntPtr platformData)
                {
                    cmd.ConfigureFoveatedRendering(platformData);
                }

                public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass, MaterialPropertyBlock properties)
                {
                    cmd.DrawMesh(mesh, matrix, material, submeshIndex, shaderPass, properties);
                }

                public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass)
                {
                    cmd.DrawMesh(mesh, matrix, material, submeshIndex, shaderPass);
                }

                public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex)
                {
                    cmd.DrawMesh(mesh, matrix, material, submeshIndex);
                }

                public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material)
                {
                    cmd.DrawMesh(mesh, matrix, material);
                }

                public void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties)
                {
                    cmd.DrawMeshInstanced(mesh, submeshIndex, material, shaderPass, matrices, count, properties);
                }

                public void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, int count)
                {
                    cmd.DrawMeshInstanced(mesh, submeshIndex, material, shaderPass, matrices, count);
                }

                public void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices)
                {
                    cmd.DrawMeshInstanced(mesh, submeshIndex, material, shaderPass, matrices);
                }

                public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
                {
                    cmd.DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset, properties);
                }

                public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs, int argsOffset)
                {
                    cmd.DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset);
                }

                public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs)
                {
                    cmd.DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs);
                }

                public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, GraphicsBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
                {
                    cmd.DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset, properties);
                }

                public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, GraphicsBuffer bufferWithArgs, int argsOffset)
                {
                    cmd.DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset);
                }

                public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, GraphicsBuffer bufferWithArgs)
                {
                    cmd.DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs);
                }

                public void DrawMeshInstancedProcedural(Mesh mesh, int submeshIndex, Material material, int shaderPass, int count, MaterialPropertyBlock properties)
                {
                    cmd.DrawMeshInstancedProcedural(mesh, submeshIndex, material, shaderPass, count, properties);
                }

                public void DrawMultipleMeshes(Matrix4x4[] matrices, Mesh[] meshes, int[] subsetIndices, int count, Material material, int shaderPass, MaterialPropertyBlock properties)
                {
                    cmd.DrawMultipleMeshes(matrices, meshes, subsetIndices, count, material, shaderPass, properties);
                }

                public void DrawOcclusionMesh(RectInt normalizedCamViewport)
                {
                    cmd.DrawOcclusionMesh(normalizedCamViewport);
                }

                public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, int instanceCount, MaterialPropertyBlock properties)
                {
                    cmd.DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount, properties);
                }

                public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, int instanceCount)
                {
                    cmd.DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount);
                }

                public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount)
                {
                    cmd.DrawProcedural(matrix, material, shaderPass, topology, vertexCount);
                }

                public void DrawProcedural(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int indexCount, int instanceCount, MaterialPropertyBlock properties)
                {
                    cmd.DrawProcedural(indexBuffer, matrix, material, shaderPass, topology, indexCount, instanceCount, properties);
                }

                public void DrawProcedural(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int indexCount, int instanceCount)
                {
                    cmd.DrawProcedural(indexBuffer, matrix, material, shaderPass, topology, indexCount, instanceCount);
                }

                public void DrawProcedural(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int indexCount)
                {
                    cmd.DrawProcedural(indexBuffer, matrix, material, shaderPass, topology, indexCount);
                }

                public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
                {
                    cmd.DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
                }

                public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset)
                {
                    cmd.DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset);
                }

                public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs)
                {
                    cmd.DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs);
                }

                public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
                {
                    cmd.DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
                }

                public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset)
                {
                    cmd.DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, argsOffset);
                }

                public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs)
                {
                    cmd.DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs);
                }

                public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
                {
                    cmd.DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
                }

                public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset)
                {
                    cmd.DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset);
                }

                public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs)
                {
                    cmd.DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs);
                }

                public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties)
                {
                    cmd.DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
                }

                public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset)
                {
                    cmd.DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, argsOffset);
                }

                public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs)
                {
                    cmd.DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs);
                }

                public void DrawRenderer(Renderer renderer, Material material, int submeshIndex, int shaderPass)
                {
                    cmd.DrawRenderer(renderer, material, submeshIndex, shaderPass);
                }

                public void DrawRenderer(Renderer renderer, Material material, int submeshIndex)
                {
                    cmd.DrawRenderer(renderer, material, submeshIndex);
                }

                public void DrawRenderer(Renderer renderer, Material material)
                {
                    cmd.DrawRenderer(renderer, material);
                }

                public void DrawRendererList(RendererList rendererList)
                {
                    cmd.DrawRendererList(rendererList);
                }

                public void SetFoveatedRenderingMode(FoveatedRenderingMode foveatedRenderingMode)
                {
                    cmd.SetFoveatedRenderingMode(foveatedRenderingMode);
                }

                public void SetInstanceMultiplier(uint multiplier)
                {
                    cmd.SetInstanceMultiplier(multiplier);
                }

                public void SetWireframe(bool enable)
                {
                    cmd.SetWireframe(enable);
                }
            }

            private static RasterCommandBufferWrapper s_rasterCommandBufferWrapper = new RasterCommandBufferWrapper();

            private static void ExecutePass(PassData data, RasterGraphContext context)
            {
                var list = data.Cameras;
                
                if (data.ClearDepth)
                {
                    context.cmd.ClearRenderTarget(true, false, Color.black);
                }

                for (int i = 0; i < list.Count; ++i)
                {
                    var rteCamera = list[i];

                    s_rasterCommandBufferWrapper.cmd = context.cmd;
                    rteCamera.RTECommandBufferOverride = s_rasterCommandBufferWrapper;
                    rteCamera.RefreshCommandBuffer();
                    rteCamera.RTECommandBufferOverride = null;
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Render Gizmos", out var passData))
                {
                    UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();

                    passData.Cameras = GetCameraList(cameraData.camera, renderPassEvent);
                    passData.ClearDepth = renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing;

                    UniversalResourceData frameData = frameContext.Get<UniversalResourceData>();
                    builder.SetRenderAttachment(frameData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(frameData.activeDepthTexture, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
                }
            }

            private static IList<IRTECamera> m_empty = new List<IRTECamera>();
            protected IList<IRTECamera> GetCameraList(Camera camera, RenderPassEvent renderPassEvent)
            {
                if (!m_eventToCameras.TryGetValue(renderPassEvent, out var cameras))
                {
                    return m_empty;
                }

                if (!cameras.TryGetValue(camera, out var rteCameraList))
                {
                    return m_empty;
                }

                return rteCameraList;
            }
#endif
        }
    }
}

