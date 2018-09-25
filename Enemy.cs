using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IrrlichtLime;
using IrrlichtLime.Core;
using IrrlichtLime.Scene;
using IrrlichtLime.Video;
using IrrKlang;


namespace Canardstein
{
    class Enemy:Thing
    {
        private int frame = 0;
        private float frameInterval = 0.15f;
        private int lifes = 10;

        public override void Update(float elapsedSec, CameraSceneNode camera)
        {
            if (this.lifes <= 0)
            {
                if (this.frame < 4)
                {
                    this.frame = 4;
                }
                this.frameInterval -= elapsedSec;
                if (this.frameInterval < 0)
                {
                    this.frameInterval = 0.15f;
                    this.frame++;
                    if (this.frame > 6)
                    {
                        this.frame = 6;
                    }
                    this.sprite.SetMaterialTexture(0, this.game.enemyTextures[this.frame]);
                }
            }
            else
            {
                this.game.TryToMove(this.sprite, (camera.Position - this.sprite.Position) * elapsedSec * .25f);
                this.frameInterval -= elapsedSec;
                if (this.frameInterval < 0)
                {
                    this.frameInterval = 0.15f;
                    this.frame++;
                    if (this.frame > 1)
                    {
                        this.frame = 0;
                    }
                    this.sprite.SetMaterialTexture(0, this.game.enemyTextures[this.frame]);
                }
            }
        }

        public override void Damage(int damage)
        {
            this.lifes -= damage;
        }
    }
}
