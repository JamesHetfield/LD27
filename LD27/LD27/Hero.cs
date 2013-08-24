﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LD27
{
    public class Hero
    {
        public Vector3 Position;
        public float Rotation;
        public Vector3 Speed;

        public int RoomX;
        public int RoomY;

        VoxelSprite spriteSheet;

        BasicEffect drawEffect;

        float moveSpeed = 0.5f;

        double frameTime = 0;
        double frameTargetTime = 100;
        int currentFrame = 0;

        public Hero(int startX, int startY, Vector3 pos)
        {
            RoomX = startX;
            RoomY = startY;

            Position = pos;
        }

        public void LoadContent(ContentManager content, GraphicsDevice gd)
        {
            spriteSheet = new VoxelSprite(16, 16, 16);
            LoadVoxels.LoadSprite(Path.Combine(content.RootDirectory, "dude.vxs"), ref spriteSheet);

            drawEffect = new BasicEffect(gd)
            {
                VertexColorEnabled = true
            };
        }

        public void Update(GameTime gameTime, Camera gameCamera, Room currentRoom, List<Door> doors, ref Room[,] rooms)
        {
            CheckCollisions(currentRoom.World, doors);
            Position += Speed;

            Vector2 v2pos = new Vector2(Position.X, Position.Y);
            Vector2 v2speed = new Vector2(Speed.X, Speed.Y);
            if (Speed.Length() > 0f)
            {
                frameTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                if (frameTime >= frameTargetTime)
                {
                    frameTime = 0;
                    currentFrame++;
                    if (currentFrame == 4) currentFrame = 0;
                }
                Rotation = Helper.TurnToFace(v2pos, v2pos + (v2speed * 50f), Rotation, 1f, 0.5f);
            }

            if (Position.X < doors[3].Position.X) { RoomX--; Position = doors[1].Position + new Vector3(0f, 0f, 4f); ResetDoors(doors, ref rooms); }
            if (Position.X > doors[1].Position.X) { RoomX++; Position = doors[3].Position + new Vector3(0f, 0f, 4f); ResetDoors(doors, ref rooms); }
            if (Position.Y < doors[0].Position.Y) { RoomY--; Position = doors[2].Position + new Vector3(0f, 0f, 4f); ResetDoors(doors, ref rooms); }
            if (Position.Y > doors[2].Position.Y) { RoomY++; Position = doors[0].Position + new Vector3(0f, 0f, 4f); ResetDoors(doors, ref rooms); }

            Vector2 p = Helper.RandomPointInCircle(Helper.PointOnCircle(ref v2pos, 1f, (Rotation - MathHelper.Pi) + 0.1f), 0f, 2f);
            ParticleController.Instance.Spawn(new Vector3(p, Position.Z-1f), new Vector3(0f, 0f, -0.01f - ((float)Helper.Random.NextDouble() * 0.01f)), 0.5f, Color.Black*0.2f, 2000, false);

            drawEffect.Projection = gameCamera.projectionMatrix;
            drawEffect.View = gameCamera.viewMatrix;
            drawEffect.World = gameCamera.worldMatrix *
                               Matrix.CreateRotationX(MathHelper.PiOver2) *
                               Matrix.CreateRotationZ(Rotation - MathHelper.PiOver2) *
                               Matrix.CreateTranslation(new Vector3(0, 0, (-(spriteSheet.Z_SIZE * SpriteVoxel.HALF_SIZE)) + SpriteVoxel.HALF_SIZE)) *
                               Matrix.CreateScale(0.9f) *
                               Matrix.CreateTranslation(Position);
        }

        private void ResetDoors(List<Door> doors, ref Room[,] rooms)
        {
            if (RoomX > 0 && !rooms[RoomX - 1, RoomY].IsGap) doors[3].Open(true); else doors[3].Close(true);
            if (RoomX < 3 && !rooms[RoomX + 1, RoomY].IsGap) doors[1].Open(true); else doors[1].Close(true);
            if (RoomY > 0 && !rooms[RoomX, RoomY - 1].IsGap) doors[0].Open(true); else doors[0].Close(true);
            if (RoomY < 3 && !rooms[RoomX, RoomY + 1].IsGap) doors[2].Open(true); else doors[2].Close(true);

            ParticleController.Instance.Reset();
        }

        public void Draw(GraphicsDevice gd, Camera gameCamera)
        {

            foreach (EffectPass pass in drawEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                AnimChunk c = spriteSheet.AnimChunks[currentFrame];
                gd.DrawUserIndexedPrimitives<VertexPositionNormalColor>(PrimitiveType.TriangleList, c.VertexArray, 0, c.VertexArray.Length, c.IndexArray, 0, c.VertexArray.Length / 2);
            }
        }

        public void Move(Vector2 virtualJoystick)
        {
            Vector3 dir = new Vector3(virtualJoystick, 0f);
            Speed = dir * moveSpeed;
        }

        void CheckCollisions(VoxelWorld world, List<Door> doors)
        {
            float checkRadius = 3f;
            float radiusSweep = 0.75f;
            Vector2 v2Pos = new Vector2(Position.X, Position.Y);
            float checkHeight = Position.Z - 3f;
            Voxel checkVoxel;
            Vector3 checkPos;

            if (Speed.Y < 0f)
            {
                for (float a = -MathHelper.PiOver2 - radiusSweep; a < -MathHelper.PiOver2 + radiusSweep; a += 0.02f)
                {
                    checkPos = new Vector3(Helper.PointOnCircle(ref v2Pos, checkRadius, a), checkHeight);
                    checkVoxel = world.GetVoxel(checkPos);
                    if ((checkVoxel.Active && world.CanCollideWith(checkVoxel.Type)))
                    {
                        Speed.Y = 0f;
                    }
                    foreach (Door d in doors) { if (d.IsBlocked && d.CollisionBox.Contains(checkPos)==ContainmentType.Contains) Speed.Y = 0f; }
                }
            }
            if (Speed.Y > 0f)
            {
                for (float a = MathHelper.PiOver2 - radiusSweep; a < MathHelper.PiOver2 + radiusSweep; a += 0.02f)
                {
                    checkPos = new Vector3(Helper.PointOnCircle(ref v2Pos, checkRadius, a), checkHeight);
                    checkVoxel = world.GetVoxel(checkPos);
                    if ((checkVoxel.Active && world.CanCollideWith(checkVoxel.Type)))
                    {
                        Speed.Y = 0f;
                    }
                    foreach (Door d in doors) { if (d.IsBlocked && d.CollisionBox.Contains(checkPos) == ContainmentType.Contains) Speed.Y = 0f; }

                }
            }
            if (Speed.X < 0f)
            {
                for (float a = -MathHelper.Pi - radiusSweep; a < -MathHelper.Pi + radiusSweep; a += 0.02f)
                {
                    checkPos = new Vector3(Helper.PointOnCircle(ref v2Pos, checkRadius, a), checkHeight);
                    checkVoxel = world.GetVoxel(checkPos);
                    if ((checkVoxel.Active && world.CanCollideWith(checkVoxel.Type)))
                    {
                        Speed.X = 0f;
                    }
                    foreach (Door d in doors) { if (d.IsBlocked && d.CollisionBox.Contains(checkPos) == ContainmentType.Contains) Speed.X = 0f; }

                }
            }
            if (Speed.X > 0f)
            {
                for (float a = -radiusSweep; a < radiusSweep; a += 0.02f)
                {
                    checkPos = new Vector3(Helper.PointOnCircle(ref v2Pos, checkRadius, a), checkHeight);
                    checkVoxel = world.GetVoxel(checkPos);
                    if ((checkVoxel.Active && world.CanCollideWith(checkVoxel.Type)))
                    {
                        Speed.X = 0f;
                    }
                    foreach (Door d in doors) { if (d.IsBlocked && d.CollisionBox.Contains(checkPos) == ContainmentType.Contains) Speed.X = 0f; }

                }
            }
        }

        internal void TryPlantBomb(Room currentRoom)
        {
            BombController.Instance.Spawn(Position + new Vector3(0f, 0f, -3f), currentRoom);
        }
    }
}
