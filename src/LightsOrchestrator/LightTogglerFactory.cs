namespace LightsOrchestrator
{
    using Microsoft.Extensions.DependencyInjection;

    public class LightTogglerFactory
    {
        public ILightToggler Create()
        {
            return Program.container.GetService<ILightToggler>();
        }
    }
}