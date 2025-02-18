﻿using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace Tilegam.Client.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ParticleInstanceStatic
    {
        public Vector4 InitialPosition;
        public Vector4 Color;

        public override string ToString()
        {
            return InitialPosition.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ParticleInstanceDynamic
    {
        public Vector4 Position;
        public Vector4 Velocity;
        public Vector4 Scale;
        public float Time;

        public override string ToString()
        {
            return Position.ToString();
        }
    }

    internal class ParticlePlane : Renderable
    {
        private static ushort[] s_quadIndices = new ushort[] { 0, 1, 2, 0, 2, 3 };

        private static Vector3[] s_quadVertices = new Vector3[] {
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, 0, 0)
        };

        private DisposeCollector _disposeCollector;
        private Pipeline _pipeline;
        private ResourceSet _sharedResourceSet;
        private DeviceBuffer _cameraInfoBuffer;
        private DeviceBuffer _ib;
        private DeviceBuffer _vb;
        private DeviceBuffer _instanceStaticVb;
        private DeviceBuffer _instanceDynamicVb;

        public Camera Camera { get; }

        ParticleInstanceStatic[] particlesStatic;
        ParticleInstanceDynamic[] particlesDynamic;

        public ParticlePlane(Camera camera)
        {
            Camera = camera ?? throw new ArgumentNullException(nameof(camera));

            particlesStatic = new ParticleInstanceStatic[0_100_000];
            particlesDynamic = new ParticleInstanceDynamic[particlesStatic.Length];

            for (int i = 0; i < particlesStatic.Length; i++)
            {
                ref ParticleInstanceStatic particleStatic = ref particlesStatic[i];
                ref ParticleInstanceDynamic particleDynamic = ref particlesDynamic[i];

                //if (i > 250000)
                //{
                //    particle.InitialPosition = GetRandomVec4() * range - new Vector4(range / 2f, 0, range / 2f, 0);
                //}
                //else
                {
                    Vector3 unit = rng.NextUnitVector3();

                    particleStatic.InitialPosition = new Vector4(unit * range / 2f, 0);
                    particleStatic.InitialPosition.Y += range / 2f;
                }
                particleStatic.InitialPosition.W = 0;

                particleDynamic.Position = particleStatic.InitialPosition;

                particleStatic.Color = new Vector4(i / (float)particlesStatic.Length, 0, 0, 0);

                particleDynamic.Scale = new Vector4(new Vector3(1), 1);
            }
        }

        public Vector4 GetRandomVec4()
        {
            return new Vector4(
                rng.NextSingle(),
                rng.NextSingle(),
                rng.NextSingle(),
                1);
        }

        FastRandom rng = new FastRandom(1234);
        float range = 7;

        float waveRange = 10;
        float lerpAmount = 0;

        public void Update(in FrameTime time)
        {
            float delta = time.DeltaSeconds;

            Vector4 acceleration = new Vector4(0, -100f, 0, 0);

            float halfRange = range / 2f;
            BoundingBox box = new BoundingBox(
                new Vector3(-halfRange, 0, -halfRange),
                new Vector3(halfRange, 0, halfRange));

            Ray ray = new Ray(Camera.Position, Camera.LookDirection);
            bool intersect = ray.Intersects(box, out float distance);
            Vector3 raypoint = ray.GetPoint(distance);
            Vector4 raypoint4 = new(raypoint, 1);

            //ImGuiNET.ImGui.Begin("Particle Plane");
            //
            //ImGuiNET.ImGui.Text(intersect.ToString());
            //ImGuiNET.ImGui.Text(distance.ToString());
            //
            //ImGuiNET.ImGui.SliderFloat("Wave Range", ref waveRange, 0, 1000);
            //ImGuiNET.ImGui.SliderFloat("Lerp Amount", ref lerpAmount, 0, 1);
            //
            //ImGuiNET.ImGui.End();

            //for (int i = 0; i < particles.Length; i++)
            //{
            //    ref ParticleInstance particle = ref particles[i];
            //
            //    ref Vector4 position = ref particle.Position;
            //
            //    if (intersect)
            //    {
            //        float distanceToRayPoint = Vector4.DistanceSquared(position, raypoint4);
            //        if (distanceToRayPoint < waveRange * waveRange)
            //        {
            //            particle.Velocity += new Vector4(0, (500 - acceleration.Y) * delta, 0, 0);
            //
            //            particle.Time = lerpAmount;
            //        }
            //    }
            //
            //    particle.Velocity += acceleration * delta;
            //
            //    position += particle.Velocity * delta;
            //
            //    if (position.Y < 0f)
            //    {
            //        position.Y = 0f;
            //        particle.Velocity = Vector4.Zero;
            //    }
            //
            //    position = Vector4.Lerp(position, particle.InitialPosition, particle.Time);
            //
            //    //position = Vector4.Lerp(position, target, delta);
            //    //
            //    //float distSqr = Vector4.DistanceSquared(position, target);
            //    //if (distSqr < 1f)
            //    //{
            //    //    position = GetRandomVec4() * range - new Vector4(range / 2f);
            //    //}
            //}

            //if (lerpAmount == 0)
            //{
            //    for (int i = 0; i < particlesDynamic.Length; i++)
            //    {
            //        ref ParticleInstanceDynamic particle = ref particlesDynamic[i];
            //
            //        ref Vector4 position = ref particle.Position;
            //
            //        if (intersect)
            //        {
            //            float distanceToRayPoint = Vector4.DistanceSquared(position, raypoint4);
            //            if (distanceToRayPoint < waveRange * waveRange)
            //            {
            //                if (rng.NextSingle() < 0.5f)
            //                {
            //                    particle.Velocity += new Vector4(0, (500 - acceleration.Y) * delta, 0, 0);
            //                }
            //            }
            //        }
            //
            //        particle.Velocity += acceleration * delta;
            //
            //        position += particle.Velocity * delta;
            //
            //        if (position.Y < 0f)
            //        {
            //            position.Y = 0f;
            //            particle.Velocity = Vector4.Zero;
            //        }
            //    }
            //}
            //else
            //{
            //    for (int i = 0; i < particlesDynamic.Length; i++)
            //    {
            //        ref ParticleInstanceStatic particleStatic = ref particlesStatic[i];
            //        ref ParticleInstanceDynamic particleDynamic = ref particlesDynamic[i];
            //
            //        particleDynamic.Time = lerpAmount;
            //
            //        ref Vector4 position = ref particleDynamic.Position;
            //
            //        position = Vector4.Lerp(position, particleStatic.InitialPosition, particleDynamic.Time);
            //    }
            //}

            float total = time.TotalSeconds * 5;

            for (int i = 0; i < particlesDynamic.Length / 2; i++)
            {
                ref ParticleInstanceStatic pstat = ref particlesStatic[i];
                ref ParticleInstanceDynamic pdyna = ref particlesDynamic[i];

                ref Vector4 position = ref pdyna.Position;

                position = new Vector4(
                    MathF.Cos(total + i * 0.003f) * 64 + MathF.Sin(total * pstat.InitialPosition.X + i) * 128,
                    i * 0.01f + MathF.Sin(total * 8 + i) * 64 - 500,
                    MathF.Sin(total + i * 0.003f) * 64 + MathF.Cos(total * pstat.InitialPosition.Y + i) * 128,
                    0);
            }

            for (int i = particlesDynamic.Length / 2; i < particlesDynamic.Length; i++)
            {
                ref ParticleInstanceStatic pstat = ref particlesStatic[i];
                ref ParticleInstanceDynamic pdyna = ref particlesDynamic[i];

                ref Vector4 position = ref pdyna.Position;

                int j = i - particlesDynamic.Length / 2;
                position = new Vector4(
                    MathF.Sin(total + j * 0.003f) * 64 + MathF.Cos(total * pstat.InitialPosition.X + j) * 128,
                    j * 0.01f + MathF.Cos(total * 8 + j) * 64 - 500,
                    MathF.Cos(total + j * 0.003f) * 64 + MathF.Sin(total * pstat.InitialPosition.Y + j) * 128,
                    0);
            }
        }

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            DisposeCollectorResourceFactory factory = new(gd.ResourceFactory);
            _disposeCollector = factory.DisposeCollector;

            ShaderSet shaderSet = sc.ShaderCache.GetShaders(
                gd, gd.ResourceFactory, AssetHelper.GetShaderPath("ParticlePlane"));

            VertexLayoutDescription sharedVertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

            VertexLayoutDescription vertexLayoutPerInstanceStatic = new VertexLayoutDescription(
                new VertexElementDescription("InstanceInitialPosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("InstanceColor", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
            vertexLayoutPerInstanceStatic.InstanceStepRate = 1;

            VertexLayoutDescription vertexLayoutPerInstanceDynamic = new VertexLayoutDescription(
                new VertexElementDescription("InstancePosition", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("InstanceVelocity", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("InstanceTime", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1));
            vertexLayoutPerInstanceDynamic.InstanceStepRate = 1;

            ResourceLayoutElementDescription[] resourceLayoutElementDescriptions =
            {
                new ResourceLayoutElementDescription("CameraInfo", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            };
            ResourceLayoutDescription resourceLayoutDescription = new ResourceLayoutDescription(resourceLayoutElementDescriptions);
            ResourceLayout sharedLayout = factory.CreateResourceLayout(resourceLayoutDescription);

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                new BlendStateDescription(
                    RgbaFloat.Black,
                    BlendAttachmentDescription.OverrideBlend,
                    BlendAttachmentDescription.OverrideBlend),
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet.CreateDescription(
                    sharedVertexLayout,
                    vertexLayoutPerInstanceStatic,
                    vertexLayoutPerInstanceDynamic),
                new ResourceLayout[] { sharedLayout },
                sc.MainSceneFramebuffer.OutputDescription);
            _pipeline = factory.CreateGraphicsPipeline(ref pd);

            _cameraInfoBuffer = factory.CreateBuffer(
                new BufferDescription((uint)Unsafe.SizeOf<CameraInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            
            _vb = factory.CreateBuffer(
                new BufferDescription((uint)s_quadVertices.SizeInBytes(), BufferUsage.VertexBuffer));
            cl.UpdateBuffer(_vb, 0, s_quadVertices);

            _ib = factory.CreateBuffer(
                new BufferDescription((uint)s_quadIndices.SizeInBytes(), BufferUsage.IndexBuffer));
            cl.UpdateBuffer(_ib, 0, s_quadIndices);

            _instanceStaticVb = factory.CreateBuffer(new BufferDescription(particlesStatic.SizeInBytes(), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            cl.UpdateBuffer(_instanceStaticVb, 0, particlesStatic);

            _instanceDynamicVb = factory.CreateBuffer(new BufferDescription(particlesDynamic.SizeInBytes(), BufferUsage.VertexBuffer | BufferUsage.Dynamic));

            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(sharedLayout, new[] { _cameraInfoBuffer });
            _sharedResourceSet = factory.CreateResourceSet(resourceSetDescription);
        }

        public override void DestroyDeviceObjects()
        {
            _disposeCollector.DisposeAll();
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey();
        }

        public override void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass)
        {
            Matrix4x4 viewMatrix = Camera.ViewMatrix;
            Matrix4x4.Invert(viewMatrix, out Matrix4x4 inversedViewMatrix);
            cl.UpdateBuffer(_cameraInfoBuffer, 0, new CameraInfo(Camera.ProjectionMatrix, viewMatrix, inversedViewMatrix));
            cl.UpdateBuffer(_instanceDynamicVb, 0, particlesDynamic);

            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, _sharedResourceSet);
            cl.SetVertexBuffer(0, _vb);
            cl.SetVertexBuffer(1, _instanceStaticVb);
            cl.SetVertexBuffer(2, _instanceDynamicVb);
            cl.SetIndexBuffer(_ib, IndexFormat.UInt16);
            cl.DrawIndexed((uint)s_quadIndices.Length, (uint)particlesDynamic.Length, 0, 0, 0);
        }

        public override RenderPasses RenderPasses => RenderPasses.Opaque;

        public override void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
        }

        public struct CameraInfo
        {
            public Matrix4x4 Projection;
            public Matrix4x4 View;
            public Matrix4x4 InverseView;

            public CameraInfo(Matrix4x4 projection, Matrix4x4 view, Matrix4x4 inverseView)
            {
                Projection = projection;
                View = view;
                InverseView = inverseView;
            }
        }
    }
}
