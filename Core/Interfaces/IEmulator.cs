using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


public interface IEmulator{
    void Initialize();
    void LoadContent();
    void Update(GameTime gameTime);
    void Draw();
}