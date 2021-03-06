﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IrrlichtLime;
using IrrlichtLime.Core;
using IrrlichtLime.Scene;
using IrrlichtLime.Video;
using IrrKlang;
using log4net;

namespace Canardstein
{
    class Enemy:Thing
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private int frame = 0;
        private float frameInterval = 0.15f;
        private int lifes = 10;
        private float nextAttack = 1.5f;

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
                this.frameInterval -= elapsedSec;
                this.nextAttack -= elapsedSec;

                if (this.nextAttack <= 0.0f)
                {
                    if (this.frameInterval < 0)
                    {
                        if (this.frame < 2)
                        {
                            this.frame = 2;
                        }
                        else if (this.frame == 2)
                        {
                            Attack();
                            this.frame = 3;
                        }
                        else
                        {
                            this.frame = 0;
                            this.nextAttack = 1.5f;
                        }
                        this.frameInterval = 0.15f;
                        this.sprite.SetMaterialTexture(0, this.game.enemyTextures[this.frame]);
                    }
                }
                else
                {
                    this.game.TryToMove(this.sprite, (camera.Position - this.sprite.Position) * elapsedSec * .25f);
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
        }

        private void Attack()
        {
            this.game.audio.Play2D("res/gun.wav");
            this.game.lifes -= 5;
            this.game.Device.SetWindowCaption("VIES:" + this.game.lifes.ToString() + "%");
        }

        public override void Damage(int damage)
        {
            this.lifes -= damage;
            Logger.Debug("- Points de vie restants : " + this.lifes.ToString());
        }
    }
}
