using System;
using System.Drawing;
using System.Collections.Generic;

using IrrlichtLime;
using IrrlichtLime.Core;
using IrrlichtLime.Scene;
using IrrlichtLime.Video;
using IrrKlang;
using log4net;

namespace Canardstein
{
    public class Game
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //lance le jeu
        private static void Main(string[] args) { Game jeu = new Game(); }

        private IrrlichtDevice device;
        public IrrlichtDevice Device
        {
            get { return device; }
        }
        //pour gérer le debit des frames
        private uint lastFrame = 0;
        //pour le controle au clavier   
        private bool keyUp, keyDown, keyLeft, keyRight;
        //pour gérer la rotation
        private double rotation = 0;
        private Vector3Df vectorUp = new Vector3Df(1, 0, 0);
        private Vector3Df vectorRight = new Vector3Df(0, 0, -1);
        //level
        private Texture textureWall, textureGround, textureCeilling, textureWallAlter;
        private bool[,] walls = new bool[32,32];
        //gun
        private Texture[] gunTextures = new Texture[3];
        private int gunFrame = 0;
        private float gunNextFrame = 0.1f;  
        private ISoundEngine audio;
        //enemis
        private List<Thing> things = new List<Thing>();
        public Texture[] enemyTextures = new Texture[7];

        //la lib irrLicht a besoin d'une liste de couleur correspondant aux 4 coins de l'image qu'il affiche    
        private readonly List<IrrlichtLime.Video.Color> whiteColors = new List<IrrlichtLime.Video.Color>(new IrrlichtLime.Video.Color[] { IrrlichtLime.Video.Color.OpaqueWhite, IrrlichtLime.Video.Color.OpaqueWhite, IrrlichtLime.Video.Color.OpaqueWhite, IrrlichtLime.Video.Color.OpaqueWhite });

        public Game()
        {
            Logger.Info("Initialisation du jeu");
            //Definit la fenetre direct3D
            this.device = IrrlichtDevice.CreateDevice(
                DriverType.Direct3D9,
                new Dimension2Di(800, 600),
                32, false, false, true);
            //initialise le module audio
            this.audio = new ISoundEngine();
            //charge les textures
            this.textureWall = this.device.VideoDriver.GetTexture("res/wall.png");  
            this.textureGround = this.device.VideoDriver.GetTexture("res/ground.png");
            this.textureCeilling = this.device.VideoDriver.GetTexture("res/ceiling.png");
            this.textureWallAlter = this.device.VideoDriver.GetTexture("res/wall_alter.png");
            //pistolet
            for (int i = 0; i < 3; i++)
            {
                this.gunTextures[i] = this.device.VideoDriver.GetTexture("res/gun_" + i.ToString() + ".png");
            }
            //enemi
            for (int i = 0; i < 7; i++)
            {
                this.enemyTextures[i] = Device.VideoDriver.GetTexture("res/enemy_" + i.ToString() + ".png");
            }
            //plan de 32*32 fois la meme texture
            Mesh meshGround = this.device.SceneManager.AddHillPlaneMesh("plan", new Dimension2Df(1, 1), new Dimension2Di(32, 32), null, 0, new Dimension2Df(0, 0), new Dimension2Df(32, 32));

            MeshSceneNode ground = this.device.SceneManager.AddMeshSceneNode(meshGround);
            ground.SetMaterialFlag(MaterialFlag.Lighting, false);
            ground.SetMaterialTexture(0, this.textureGround);
            //place le sol 0.5 unité sous la camera. 15.5 c'est pour avoir un coin en 0 0
            ground.Position = new Vector3Df(15.5f, -0.5f, 15.5f);

            MeshSceneNode ceiling = this.device.SceneManager.AddMeshSceneNode(meshGround);
            ceiling.SetMaterialFlag(MaterialFlag.Lighting, false);
            ceiling.SetMaterialTexture(0, this.textureCeilling);
            //place le sol 0.5 unité au dessus de la camera. 15.5 c'est pour avoir un coin en 0 0
            ceiling.Position = new Vector3Df(15.5f, 0.5f, 15.5f);
            //on le tourne vers le bas
            ceiling.Rotation = new Vector3Df(180, 0, 0);


            this.device.SetWindowCaption("Canardstein 3D");
            //A chaque evenement, on appelle ManageEvent
            this.device.OnEvent += ManageEvent;

            LoadMap();

            //Ajoute une camera
            CameraSceneNode camera = this.device.SceneManager.AddCameraSceneNode(null, new Vector3Df(1, 0, 1), new Vector3Df(2, 0, 1));
            camera.NearValue = 0.1f;
            //place le curseur au milieu de la fenetre
            this.device.CursorControl.Position = new Vector2Di(400, 300);
            //rend le curseur invisible
            this.device.CursorControl.Visible = false;

            AddThings<Enemy>(3, 3);
            Logger.Info("Jeu initialisé");
            while (this.device.Run())
            {
                //temps écoulé en s depuis la dernière frame
                float elapsedSec = (this.device.Timer.Time - this.lastFrame) / 1000f;
                //temps actuel en ms
                this.lastFrame = this.device.Timer.Time;
                //si le curseur n'est plus au centre de la fenetre
                if (this.device.CursorControl.Position.X != 400)
                {
                    //calcule l'ecart entre le centre de la fenetre et la position du curseur
                    double cursorPosX = this.device.CursorControl.Position.X - 400;
                    //ajoute cet ecart à la rotation (multiplie par 0.0025 pour la sensibilité)
                    this.rotation += cursorPosX * 0.0025;
                    //replace le curseur au centre la fenetre
                    this.device.CursorControl.Position = new Vector2Di(400, 300);
                    //recalcule vecteur avant
                    this.vectorUp = new Vector3Df((float)Math.Cos(this.rotation), 0, -(float)Math.Sin(this.rotation));
                    //recalcule vecteur droite (vecteur avant tourné à 90° à droite)
                    this.vectorRight = this.vectorUp;
                    this.vectorRight.RotateXZby(-90);
                }
                //calcule la vitesse de la camera
                Vector3Df vectorSpeed = GetSpeed(elapsedSec);
                //tente de deplacer la camera
                if (!TryToMove(camera, vectorSpeed))
                {
                    //sinon tente de le deplace uniquement sur l'axe x
                    if (!TryToMove(camera, new Vector3Df(vectorSpeed.X, 0, 0)))
                    { 
                        //sinon tente de le deplace uniquement sur l'axe z
                        TryToMove(camera, new Vector3Df(0, 0, vectorSpeed.Z));
                    }
                }
                camera.Target = camera.Position + this.vectorUp;
                this.device.VideoDriver.BeginScene(ClearBufferFlag.Color | ClearBufferFlag.Depth, IrrlichtLime.Video.Color.OpaqueMagenta);
                this.device.SceneManager.DrawAll();
                for (int i = 0; i < this.things.Count; i++)
                {
                    this.things[i].Update(elapsedSec, camera);
                }
                //dessine le pistolet au premier plan
                this.device.VideoDriver.Draw2DImage(
                this.gunTextures[gunFrame],
                    new Recti(new Vector2Di(250, 300), new Dimension2Di(300, 300)),
                    new Recti(0, 0, 512, 512), null,
                    whiteColors, true);
                AnimateGun(elapsedSec);
                this.device.VideoDriver.EndScene();
            }
        }

        /// <summary>
        /// Anime le pistolet
        /// </summary>
        /// <param name="elapsedSec"></param>
        private void AnimateGun(float elapsedSec)
        {
            //si le joueur est en train de tirer
            if (this.gunFrame > 0)
            {   
                //decrement le chrono
                this.gunNextFrame -= elapsedSec;
                if (this.gunNextFrame <= 0f)
                {
                    //poursuit l'annimation
                    this.gunFrame++;
                    if (this.gunFrame > 2)
                    {
                        //fin du tir
                        this.gunFrame = 0;
                    }
                    //delai d'animation
                    this.gunNextFrame = 0.1f;
                }
            }
        }

        private void LoadMap()
        {
            Bitmap map = (Bitmap)System.Drawing.Image.FromFile("res/map.png");
            for (int x = 0; x < 32; x++)
            { 
                for (int y = 0; y < 32; y++)
                {
                    this.walls[x,y] = false;
                    System.Drawing.Color col = map.GetPixel(x, y);
                    //blanc
                    if ((col.R == 255) && (col.G == 255) && (col.B == 255))
                    {
                        //mur
                        SceneNode cube = this.device.SceneManager.AddCubeSceneNode(1, null, 0, new Vector3Df(x, 0, y));
                        cube.SetMaterialFlag(MaterialFlag.Lighting, false);
                        cube.SetMaterialTexture(0, this.textureWall);
                        this.walls[x, y] = true;
                    }
                    //bleu
                    else if ((col.R == 0) && (col.G == 0) && (col.B == 255))
                    {
                        //mur alternatif
                        SceneNode cube = this.device.SceneManager.AddCubeSceneNode(1, null, 0, new Vector3Df(x, 0, y));
                        cube.SetMaterialFlag(MaterialFlag.Lighting, false);
                        cube.SetMaterialTexture(0, this.textureWallAlter);
                        this.walls[x, y] = true;
                    }
                }
            }
            map.Dispose();
        }

        /// <summary>
        /// Calcule la vitesse de la camera
        /// </summary>
        /// <param name="elapsedSec"></param>
        /// <returns></returns>
        private Vector3Df GetSpeed(float elapsedSec)
        {
            //vecteur de vitesse
            Vector3Df vectorSpeed = new Vector3Df();
            if (keyUp)
            {
                vectorSpeed += this.vectorUp;
            }
            else if (keyDown)
            {
                vectorSpeed -= this.vectorUp;
            }
            if (keyLeft)
            {
                vectorSpeed -= this.vectorRight;
            }
            else if (keyRight)
            {
                vectorSpeed += this.vectorRight;
            }
            //le vecteur vitesse a une longueur normalisé sur 1
            vectorSpeed = vectorSpeed.Normalize() * elapsedSec * 2;
            return vectorSpeed;
        }

        /// <summary>
        /// Gère tous les evenements captés par le moteur
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool ManageEvent(Event e)
        {
            //Evenements de type entrée clavier
            if (e.Type == EventType.Key)
            {
                switch (e.Key.Key)
                {
                    case KeyCode.KeyZ: this.keyUp = e.Key.PressedDown; break;
                    case KeyCode.KeyS: this.keyDown = e.Key.PressedDown; break;
                    case KeyCode.KeyQ: this.keyLeft = e.Key.PressedDown; break;
                    case KeyCode.KeyD: this.keyRight = e.Key.PressedDown; break;
                }
            }
            else if (e.Type == EventType.Mouse)
            {
                //clique et le joueur n'est pas déjà en train de tirer
                if ((e.Mouse.Type == MouseEventType.LeftDown) && (this.gunFrame == 0))
                {
                    //le joueur tire!
                    this.audio.Play2D("res/gun.wav");
                    Fire();
                    this.gunFrame = 1;
                    this.gunNextFrame = 0.1f;
                }
            }
            return false;
        }

        /// <summary>
        /// Gere les domages occasionnes par les armes
        /// </summary>
        private void Fire()
        {
            //algo de raytracing
            float posX = Device.SceneManager.ActiveCamera.Position.X;
            float posY = Device.SceneManager.ActiveCamera.Position.Z;
            Vector2Df pos = new Vector2Df(posX, posY);
            float speedX = this.vectorUp.X;
            float speedY = this.vectorUp.Z;
            Vector2Df speed = new Vector2Df(speedX, speedY) * 0.1f;
            Console.WriteLine(pos);
            Console.WriteLine(speed);
            for (int i = 0; i < 128; i++)
            {
                for (int j = 0; j < this.things.Count; j++)
                {
                    if (this.things[j].Position.GetDistanceFrom(pos) < 0.25f)
                    {
                        Logger.Debug(i.ToString() + " Enemmi " + j.ToString() + " touché");
                        this.things[j].Damage(5);
                        //pas d'autre domages deriere cet enemi
                        return;
                    } else
                    {
                        Logger.Debug(i.ToString() + " Enemmi " + j.ToString() + " raté (" + this.things[j].Position.GetDistanceFrom(pos).ToString() + ")");
                    }
                    pos += speed;
                }
            }
        }

        /// <summary>
        /// Tente de bouger l'objet dans la direction
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="direction"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public bool TryToMove(SceneNode obj, Vector3Df direction, float radius = .25f)
        {
            //calcule la nouvelle position
            Vector2Df newPos = new Vector2Df(obj.Position.X + direction.X + .5f, obj.Position.Z + direction.Z + .5f);
            int minX = (int)(newPos.X - radius);
            int maxX = (int)(newPos.X + radius);
            int minY = (int)(newPos.Y - radius);
            int maxY = (int)(newPos.Y + radius);
            int x, y;
            //pour toute les cases concernées
            for (x = minX; x <= maxX; x++)
                for (y = minY; y <= maxY; y++)
                {
                    //si la case est hors carte, on ne peut pas y aller
                    if ((x < 0) || (y < 0) || (x >= 32) || (x >= 32))
                    {
                        return false;
                    }
                    //idem si la case est un mur
                    if (this.walls[x, y])
                    {
                        return false;
                    }
                }
            //bouge l'objet
            obj.Position += direction;
            return true;
        }

        /// <summary>
        /// Ajoute une chose à la liste
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void AddThings<T>(int x, int y) where T : Thing, new()
        {
            T newThing = new T();
            newThing.Create(this, x, y);
            this.things.Add(newThing);
        }
    }
}

