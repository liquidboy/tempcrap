using System;
using System.Linq;
using System.Threading.Tasks;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox;
using SiliconStudio.Paradox.DataModel;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Effects.Cubemap;
using SiliconStudio.Paradox.Effects.Processors;
using SiliconStudio.Paradox.Effects.Renderers;
using SiliconStudio.Paradox.Effects.Skybox;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.EntityModel;
using SiliconStudio.Paradox.Graphics;

namespace CubemapReflection
{
    public class CubemapReflectionGame : Game
    {
        private Entity camera;
        private Entity robotEntity;

        public CubemapReflectionGame()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
            GraphicsDeviceManager.PreferredBackBufferWidth = 1280;
            GraphicsDeviceManager.PreferredBackBufferHeight = 720;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // load environment map
            var environment = Asset.Load<Texture>("environment");

            CreatePipeline(environment);
            
            // Add robot
            robotEntity = Asset.Load<Entity>("Robot_00");
            robotEntity.Transformation.Scaling = 0.03f * Vector3.One;
            robotEntity.Transformation.Translation = -5f * Vector3.UnitY;
            robotEntity.Transformation.Rotation = Quaternion.RotationY(MathUtil.Pi);
            Entities.Add(robotEntity);
            robotEntity.Get<AnimationComponent>().Play("walk");

            // add cubemaps
            var cubemapEntity = new Entity()
            {
                new CubemapSourceComponent(environment)
                {
                    IsDynamic = false,
                    InfluenceRadius = 30,
                }
            };
            Entities.Add(cubemapEntity);

            // add lights
            var directLight = new Entity()
            {
                new LightComponent()
                {
                    Type = LightType.Directional,
                    Deferred = true,
                    Intensity = 0.9f,
                    LightDirection = new Vector3(-1,-1,-1),
                    Enabled = true,
                    Color = new Color3(1,1,1)
                }
            };
            Entities.Add(directLight);

            // add camera
            var cameraTarget = new Entity();
            camera = new Entity()
            {
                new CameraComponent(cameraTarget, 1, 100)
                {
                    AspectRatio = 16f/9f,
                    VerticalFieldOfView = 1,
                    TargetUp = Vector3.UnitY
                }
            };
            Entities.Add(camera);
            RenderSystem.Pipeline.SetCamera(camera.Get<CameraComponent>());

            // Add a custom script
            Script.Add(GameScript1);
        }

        private void CreatePipeline(Texture environment)
        {
            // Processor
            Entities.Processors.Add(new CubemapSourceProcessor(GraphicsDevice));
            Entities.Processors.Add(new LightShadowProcessorDefaultBudget(GraphicsDevice, true));

            // Rendering pipeline
            RenderSystem.Pipeline.Renderers.Add(new CameraSetter(Services));

            RenderSystem.Pipeline.Renderers.Add(new RenderTargetSetter(Services)
            {
                ClearColor = Color.CornflowerBlue,
                EnableClearDepth = true,
                ClearDepth = 1f
            });

            // Create G-buffer pass
            var gbufferPipeline = new RenderPipeline("GBuffer");
            // Renders the G-buffer for opaque geometry.
            gbufferPipeline.Renderers.Add(new ModelRenderer(Services, "CubemapReflectionEffectMain.ParadoxGBufferShaderPass"));
            var gbufferProcessor = new GBufferRenderProcessor(Services, gbufferPipeline, GraphicsDevice.DepthStencilBuffer, false);

            // Add sthe G-buffer pass to the pipeline.
            RenderSystem.Pipeline.Renderers.Add(gbufferProcessor);

            var readOnlyDepthBuffer = GraphicsDevice.DepthStencilBuffer.ToDepthStencilReadOnlyTexture();

            // Performs the light prepass on opaque geometry.
            // Adds this pass to the pipeline.
            var lightDeferredProcessor = new LightingPrepassRenderer(Services, "CubemapReflectionLightingPrepass", GraphicsDevice.DepthStencilBuffer, gbufferProcessor.GBufferTexture);
            RenderSystem.Pipeline.Renderers.Add(lightDeferredProcessor);

            var iblRenderer = new LightingIBLRenderer(Services, "CubemapIBLSpecular", readOnlyDepthBuffer);
            RenderSystem.Pipeline.Renderers.Add(iblRenderer);

            GraphicsDevice.Parameters.Set(EnvironmentMapSpecularDeferredShadingKeys.SpecularIBLMap, iblRenderer.IBLTexture);

            // Sets the render targets and clear them. Also sets the viewport.
            RenderSystem.Pipeline.Renderers.Add(new RenderTargetSetter(Services)
            {
                EnableClearTarget = false,
                EnableClearDepth = false,
                RenderTarget = GraphicsDevice.BackBuffer,
                DepthStencil = GraphicsDevice.DepthStencilBuffer,
                Viewport = new Viewport(0, 0, GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height)
            });

            RenderSystem.Pipeline.Renderers.Add(new SkyboxRenderer(Services, environment));
            RenderSystem.Pipeline.Renderers.Add(new RenderStateSetter(Services) { DepthStencilState = GraphicsDevice.DepthStencilStates.DepthRead });
            RenderSystem.Pipeline.Renderers.Add(new ModelRenderer(Services, "CubemapReflectionEffectMain").AddOpaqueFilter());

            GraphicsDevice.Parameters.Set(RenderingParameters.UseDeferred, true);
        }

        private async Task GameScript1()
        {
            var dragValue = Vector2.Zero;
            var rotationFactor = 0.125f;
            var rotationUpFactor = 0.25f;
            var cameraInitPos = new Vector3(10, 0, 0);

            while (IsRunning)
            {
                // Wait next rendering frame
                await Script.NextFrame();

                // rotate camera
                dragValue = 0.95f * dragValue;
                if (Input.PointerEvents.Count > 0)
                {
                    dragValue = Input.PointerEvents.Aggregate(Vector2.Zero, (t, x) => x.DeltaPosition + t);
                }
                rotationFactor -= dragValue.X;
                rotationUpFactor += dragValue.Y;
                if (rotationUpFactor > 0.45f)
                    rotationUpFactor = 0.45f;
                else if (rotationUpFactor < -0.45f)
                    rotationUpFactor = -0.45f;
                camera.Transformation.Translation = Vector3.Transform(cameraInitPos, Quaternion.RotationZ((float) (Math.PI*rotationUpFactor))*Quaternion.RotationY((float) (2*Math.PI*rotationFactor)));

                robotEntity.Transformation.Rotation = Quaternion.RotationY((float)(2 * Math.PI * UpdateTime.Total.TotalMilliseconds / 15000));
            }
        }
    }
}
