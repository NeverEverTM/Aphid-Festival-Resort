using Godot;
using System;

public partial class FoodParticleBehaviour : GpuParticles2D
{
    public void CreateCustomParticleTexture()
    {
        Image _particleSheeet = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        Image _ogImage = Texture.GetImage();

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                int _originX = x * 4, _originY = y * 4;
                _particleSheeet.BlitRect(_ogImage, new(_originX + 1, _originY + 1, _originX + 5, _originY + 5), new(_originX, _originY));
            }
        }

        ImageTexture _newTexture = ImageTexture.CreateFromImage(_particleSheeet);
        Texture = _newTexture;
    }
}
