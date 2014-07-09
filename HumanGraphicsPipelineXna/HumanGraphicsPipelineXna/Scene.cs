﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace HumanGraphicsPipelineXna
{
    abstract class Scene
    {
        protected Vector2[] trianglePoints = new Vector2[3];
        protected Vector2[] normalisedTrianglePoints = new Vector2[3];
        protected Square[] triangleSquares = new Square[3];
        protected Line[] triangleLines = new Line[3]; //AB, BC, CA

        Texture2D gridLine;
        Texture2D windowSpaceLine;
        protected Button buttonNext;
        protected Button buttonPrevious;
        protected Button buttonPlay;

        protected int animationCounter = 0;
        protected int animationCounterLimit = 0;

        bool animating = false;

        protected enum State
        {
            PickPoint1,
            PickPoint2,
            PickPoint3,
            Animate,
        }

        protected State state = State.PickPoint1;

        public Scene()
        {
            gridLine = new Texture2D(Globals.graphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Color[] pixels = new Color[1];
            for (int i = 0; i < 1; i++)
                pixels[i] = new Color(0, 0, 0, 100);

            gridLine.SetData<Color>(pixels);

            windowSpaceLine = new Texture2D(Globals.graphicsDevice, 1, 1, false, SurfaceFormat.Color);
            pixels[0] = new Color(0, 0, 0, 255);
            windowSpaceLine.SetData<Color>(pixels);

            buttonNext = new Button(">", Fonts.font14, new Vector2(30, 30), new Vector2(Globals.viewport.X - 40, Globals.viewport.Y - 40), Color.DarkOliveGreen);
            buttonPrevious = new Button("<", Fonts.font14, new Vector2(30, 30), new Vector2(Globals.viewport.X - 100, Globals.viewport.Y - 40), Color.DarkOliveGreen);
            buttonPlay = new Button("||", Fonts.font14, new Vector2(30, 30), new Vector2(Globals.viewport.X - 70, Globals.viewport.Y - 40), Color.DarkOliveGreen);
            
        }

        public virtual void Update(GameTime gameTime)
        {
            StateChanges(gameTime);

            if (buttonPlay.IsClicked())
                animating = !animating;

            if (animating && animationCounter < animationCounterLimit)
                animationCounter++;
            else if (animationCounter >= animationCounterLimit)
                animating = false;

            if (!animating)
            {
                if (buttonNext.IsClicked() && animationCounter < animationCounterLimit)
                    animationCounter++;
                if (buttonPrevious.IsPressed())
                    animationCounter--;
            }
        }

        protected abstract void LastTrianglePointPlaced(GameTime gameTime);

        private void StateChanges(GameTime gameTime)
        {
            if (Inputs.MouseState.LeftButton == ButtonState.Released && Inputs.MouseStatePrevious.LeftButton == ButtonState.Pressed)
            {
                if (state == State.PickPoint1)
                {
                    trianglePoints[0] = new Vector2(Inputs.MouseState.X, Inputs.MouseState.Y);
                    triangleSquares[0] = new Square(new Vector2(Inputs.MouseState.X - 5, Inputs.MouseState.Y - 5), new Vector2(10, 10), Color.Green);
                    state = State.PickPoint2;
                }
                else if (state == State.PickPoint2)
                {
                    trianglePoints[1] = new Vector2(Inputs.MouseState.X, Inputs.MouseState.Y);
                    triangleSquares[1] = new Square(new Vector2(Inputs.MouseState.X - 5, Inputs.MouseState.Y - 5), new Vector2(10, 10), Color.Green);
                    state = State.PickPoint3;
                }
                else if (state == State.PickPoint3)
                {
                    trianglePoints[2] = new Vector2(Inputs.MouseState.X, Inputs.MouseState.Y);
                    triangleSquares[2] = new Square(new Vector2(Inputs.MouseState.X - 5, Inputs.MouseState.Y - 5), new Vector2(10, 10), Color.Green);

                    LastTrianglePointPlaced(gameTime);

                    triangleLines[0] = new Line(trianglePoints[0], trianglePoints[1], Color.Black, 1);
                    triangleLines[1] = new Line(trianglePoints[1], trianglePoints[2], Color.Black, 1);
                    triangleLines[2] = new Line(trianglePoints[2], trianglePoints[0], Color.Black, 1);

                    state = State.Animate;
                }
            }
        }

        protected void DrawGrid(SpriteBatch spriteBatch)
        {
            for (int i = 0; i <= (Globals.viewportHeight/Globals.pixelSize); i++)
                spriteBatch.Draw(gridLine, new Rectangle(0, i * (Globals.viewportHeight / (Globals.viewportHeight/Globals.pixelSize)), Globals.viewportWidth, 1), Color.White);

            for (int i = 0; i <= (Globals.viewportWidth / Globals.pixelSize); i++)
                spriteBatch.Draw(gridLine, new Rectangle(i * (Globals.viewportWidth / (Globals.viewportWidth / Globals.pixelSize)), 0, 1, Globals.viewportHeight), Color.White);        

            spriteBatch.Draw(windowSpaceLine, new Rectangle(Globals.viewportWidth / 2 - 2, 0, 4, Globals.viewportHeight), Color.White);
            spriteBatch.Draw(windowSpaceLine, new Rectangle(0, Globals.viewportHeight / 2 - 2, (Globals.viewportWidth), 4), Color.White);
        }

        

        public virtual void Draw(SpriteBatch spriteBatch) 
        {
            DrawGrid(spriteBatch);

            for (int i = 0; i < 3; i++)
            {
                if (trianglePoints[i] != Vector2.Zero)
                {
                    triangleSquares[i].Draw(spriteBatch);

                    float normalisedX = (trianglePoints[i].X - 0) / ((Globals.viewportWidth / 2) - 0) - 0.5f * 2;
                    float normalisedY = (trianglePoints[i].Y - 0) / ((Globals.viewportHeight / 2) - 0) - 0.5f * 2;

                    normalisedTrianglePoints[i] = new Vector2(normalisedX, normalisedY);
                    spriteBatch.DrawString(Fonts.smallFont, normalisedTrianglePoints[i].X.ToString() + ", " + normalisedTrianglePoints[i].Y.ToString(), new Vector2(trianglePoints[i].X - 10, trianglePoints[i].Y - 15), Color.White);
                }
            }

            if (trianglePoints[2] != Vector2.Zero)
            {
                for (int i = 0; i < 3; i++)
                    triangleLines[i].Draw(spriteBatch);
                ActionOnTriangleDraw(spriteBatch);
            }


            if (state == State.Animate)
            {
                buttonPrevious.Draw(spriteBatch);
                buttonPlay.Draw(spriteBatch);
                buttonNext.Draw(spriteBatch);

            }

            DrawText(spriteBatch);
        }

        protected abstract void ActionOnTriangleDraw(SpriteBatch spriteBatch);

        protected virtual void DrawText(SpriteBatch spriteBatch) {}
    }
}
