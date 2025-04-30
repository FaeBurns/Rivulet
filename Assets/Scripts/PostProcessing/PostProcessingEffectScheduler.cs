using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PostProcessing
{
    public class PostProcessingEffectScheduler : MonoBehaviour
    {
        [SerializeField]
        private PostProcessingEffectSource[] m_sources;

        public void Render(RenderTexture source, RenderTexture destination)
        {
            Stack<RenderTexture> temporaryRenderTextures = new Stack<RenderTexture>();

            RenderTexture GetTemporaryTexture(bool useDestination)
            {
                // if we need to use the destination, use the destination
                if (useDestination)
                    // use source if destination is null
                    // destination will be null if it's outputting to the screen
                    return RenderTexture.GetTemporary(destination?.descriptor ?? source.descriptor);

                // if there are no previous effects to get the texture from just use the source
                if (temporaryRenderTextures.Count == 0)
                    return RenderTexture.GetTemporary(source.descriptor);

                // use descriptor of the last effect in place of the source
                return RenderTexture.GetTemporary(temporaryRenderTextures.Peek().descriptor);
            }

            foreach (PostProcessingEffectSource effect in m_sources)
            {
                // skip this effect if it's not enabled
                if (!effect.isActiveAndEnabled)
                    continue;

                // get source and target for this effect
                RenderTexture effectSource = temporaryRenderTextures.Count == 0 ? source : temporaryRenderTextures.Peek();
                RenderTexture targetTexture = GetTemporaryTexture(effect.UseFullResolutionTarget);

                // render from source to target
                effect.Render(effectSource, targetTexture);

                // remember temporary texture
                temporaryRenderTextures.Push(targetTexture);
            }

            // if there were any effects created, blit from it to the destination
            if (temporaryRenderTextures.Any())
                Graphics.Blit(temporaryRenderTextures.Peek(), destination);
            // otherwise just blit from source to dest
            else
                Graphics.Blit(source, destination);

            // release all temporary textures
            while (temporaryRenderTextures.Any())
            {
                RenderTexture.ReleaseTemporary(temporaryRenderTextures.Pop());
            }
        }
    }
}