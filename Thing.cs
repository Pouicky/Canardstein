using IrrlichtLime.Core;
using IrrlichtLime.Scene;
using IrrlichtLime.Video;

namespace Canardstein
{
    class Thing
    {
        private bool destroyed;
        public bool Destroyed
        {
            get { return destroyed; }
            set { destroyed = value; }
        }

        public Vector2Df Position
        {
            get { return new Vector2Df(this.sprite.Position.X, this.sprite.Position.Z); }
        }
        
        protected BillboardSceneNode sprite;
        protected Game game;

        public Thing()
        {

        }

        public virtual void Update(float tempsEcoule, CameraSceneNode camera) { }

        public virtual void Damage(int damage) { }

        /// <summary>
        /// Cree l'objet
        /// </summary>
        /// <param name="game"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Create(Game game, int x, int y)
        {
            this.game = game;
            //cree le sprite et le position en x y
            this.sprite = this.game.Device.SceneManager.AddBillboardSceneNode(null);
            this.sprite.SetMaterialFlag(MaterialFlag.Lighting, false);
            this.sprite.SetMaterialType(MaterialType.TransparentAlphaChannel);
            this.sprite.SetSize(1, 1, 1);
            this.sprite.Position = new Vector3Df(x + 0.5f, 0, y + 0.5f);
        }

        /// <summary>
        /// Detruit l'objet
        /// </summary>
        public void Destroy()
        {
            if (!this.destroyed)
            {
                this.sprite.Remove();
                this.destroyed = true;
            }
        }
    }
}
