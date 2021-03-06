﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BoxMan
{
    public class XNALabel
    {
        public XNALabel(string str, SpriteFont font, Vector2 pos, float scale, XNAForm parent)
        {
            Str = str;
            Font = font;
            Pos = pos;
            Scale = scale;
            _parent = parent;
            _parent.AddLabel(this);
        }

        public void Draw()
        {
            _parent.GameMgr.DrawString(Font, Str, Pos, Color.Black, Scale);
        }

        public string Str;
        public SpriteFont Font;
        public Vector2 Pos;
        public float Scale;
        private XNAForm _parent;
    }

    public class XNAForm : FormMgr
    {
        public static int StandardTitleBarHeight = 50;

        SpriteFont _defaultFont;
        SpriteFont _font;

        protected RenderTarget2D renderTargetTitleBar;
        protected RenderTarget2D renderTargetInner;
        protected RenderTarget2D renderTargetOuter;

        Texture2D _baseTexture;
        Color _baseColor = Color.Yellow;
        Color _borderColor = Color.Gray;
        Color _titleBarColor = Color.LightYellow;

        protected int num;

        protected Rectangle _mainOuterRect;
        protected Rectangle _mainRect;
        protected Rectangle _mainTitleBarRect;
        protected Rectangle _mainInnerRect;
        Rectangle _topBorderRect;
        Rectangle _rightBorderRect;
        Rectangle _bottomBorderRect;
        Rectangle _leftBorderRect;
        Rectangle _titleBarRect;

        protected FormMgr _parent;

        bool _titleBar;

        protected string _title;

        int _x, _y, _width, _height;

        protected int _mainBorderWidth = 5;

        protected int _titleBarHeight;

        int _fontHeight;

        int _outerAbsX, _outerAbsY;
        int _innerAbsX, _innerAbsY;

        protected List<Button> _buttons;
        protected List<Clickable> _clickables;
        protected List<XNALabel> _labels;
        protected List<TextField> _textFields;

        bool Draggable = true;


        public XNAForm(int x, int y, int width, int height, FormMgr parent, string title = "XNAForm", bool titleBar = true) : base(parent.GameMgr)
        {
            _titleBar = titleBar;
            if (!titleBar)
                _titleBarHeight = 0;
            else
                _titleBarHeight = StandardTitleBarHeight;

            if (width == 0)
                width = 10;
            if (height == 0)
                height = 10;

            _parent = parent;

            renderTarget = _parent.RenderTarget;

            _initArgs(x, y, width, height, title, titleBar);

            Initialize();

            Height = height;

            parent.AddForm(this);
        }

        private void _initArgs(int x, int y, int width, int height, string title, bool titleBar)
        {
            _title = title;

            _x = x;
            _y = y;

            _mainOuterRect = new Rectangle(0, 0, width, height);
            _mainRect = new Rectangle(_x, _y, width, height);
            _mainTitleBarRect = new Rectangle(0, 0, width - 2 * _mainBorderWidth, height - 2 * _mainBorderWidth);
            _mainInnerRect = new Rectangle(0, 0, 
                                            width - _mainBorderWidth * 2, height - _mainBorderWidth * 2 - _titleBarHeight);


            Height = height;
            Width = width;
            X = x;
            Y = y;

            _setAbsXY();

        }

        private void _setAbsXY()
        {
            XAbs = X;
            YAbs = Y;

            XNAForm parent = _parent as XNAForm;

            if (parent != null)
            {
                XAbs += parent.InnerXAbs;
                YAbs += parent.InnerYAbs;

            }
        }

        public int TitleBarHeight
        {
            set
            {
                _titleBarHeight = value;
                _setAbsXY();
                _updateTitleBarRect();
            }

            get
            {
                return _titleBarHeight;
            }
        }

        public int XAbs
        {
            get
            {
                return _outerAbsX;
            }

            set
            {
                _outerAbsX = value;
                _innerAbsX = _outerAbsX + _mainBorderWidth;
            }
        }

        public int YAbs
        {
            get
            {
                return _outerAbsY;
            }

            set
            {
                _outerAbsY = value;
                _innerAbsY = _outerAbsY + _mainBorderWidth + _titleBarHeight;
            }
        }

        public int InnerXAbs
        {
            get
            {
                return _innerAbsX;
            }

            set
            {
                _innerAbsX = value;
                _outerAbsX = _innerAbsX - _mainBorderWidth;
            }
        }

        public int InnerYAbs
        {
            get
            {
                return _innerAbsY;
            }

            set
            {
                _innerAbsY = value;
                _outerAbsY = _innerAbsY - _mainBorderWidth - _titleBarHeight;
            }
        }

        public void Destroy()
        {
            _parent.RemoveForm(this);
        }

        public override void RemoveForm(XNAForm form)
        {
            var popupForm = form as PopupDialog;

            if (popupForm == null)
                base.RemoveForm(form);
            else
                _gameMgr.RemovePopup(popupForm);
        }

        public virtual void AddButton(int x, int y, Button.ButtonClickCallback callback, string text)
        {
            new BoxMan.Button(text, x, y, 100, 50, callback, this);
        }

        public virtual void AddButton(int x, int y, int width, int height, Button.ButtonClickCallback callback, string text)
        {
            new Button(text, x, y, width, height, callback, this);
        }

        public virtual void AddButton(Button button)
        {
            _buttons.Add(button);
        }

        public virtual void AddClickable(Clickable clickable)
        {
            var button = clickable as Button;

            if (button != null) 
            _buttons.Add(button);
            else 
                _clickables.Add(clickable);
        }

        public void ActivateTextField(TextField field)
        {
            DeactivateAllTextFields();

            field.Active = true;
        }

        public void DeactivateAllTextFields()
        {
            foreach(Clickable click in _clickables)
            {
                var other_field = click as TextField;

                other_field.Active = false;
            }
        }

        public virtual void AddLabel(XNALabel label)
        {
            _labels.Add(label);
        }

        public virtual void AddTextField(TextField field)
        {
            _textFields.Add(field);
        }

        public override void AddForm(XNAForm form)
        {
            var formPopup = form as PopupDialog;
            if (formPopup != null)
            {
                _gameMgr.AddPopup(formPopup);
            }
            else
                base.AddForm(form);
        }

        private void _updateBorderRects()
        {
            _topBorderRect = new Rectangle(0, 0, _mainOuterRect.Width, _mainBorderWidth);
            _rightBorderRect = new Rectangle(_mainOuterRect.Width - _mainBorderWidth, 0,
                                                _mainBorderWidth, _mainOuterRect.Height);
            _bottomBorderRect = new Rectangle(0, _mainOuterRect.Height - _mainBorderWidth, _mainOuterRect.Width, _mainBorderWidth);
            _leftBorderRect = new Rectangle(0, 0, _mainBorderWidth, _mainOuterRect.Height);
        }

        private void _updateTitleBarRect()
        {
            _titleBarRect = new Rectangle(0, 0, _mainInnerRect.Width, _titleBarHeight);
        }

        public bool MouseOnThis()
        {
            var parentForm = _parent as XNAForm;

            int innerXAbs, innerYAbs;

            innerXAbs = 0;
            innerYAbs = 0;

            if (parentForm != null)
            {
                innerXAbs = parentForm.InnerXAbs;
                innerYAbs = parentForm.InnerYAbs;
            }

            return Utilities.MouseOnRect(_mainRect, innerXAbs, innerYAbs);
        }

        protected virtual void Initialize()
        {
            _buttons = new List<Button>();
            _labels = new List<XNALabel>();
            _clickables = new List<Clickable>();
            _textFields = new List<TextField>();

            _defaultFont = _gameMgr.Content.Load<SpriteFont>("Courier New");
            _font = _defaultFont;
            _fontHeight = (int)_font.MeasureString("A").Y;

            _topBorderRect = new Rectangle(0, 0, _mainOuterRect.Width, _mainBorderWidth);
            _rightBorderRect = new Rectangle(_mainOuterRect.Width - _mainBorderWidth, 0, 
                                                _mainBorderWidth, _mainOuterRect.Height);
            _bottomBorderRect = new Rectangle(0, _mainOuterRect.Height - _mainBorderWidth, _mainOuterRect.Width, _mainBorderWidth);
            _leftBorderRect = new Rectangle(0, 0, _mainBorderWidth, _mainOuterRect.Height);

            _titleBarRect = new Rectangle(0, 0, _mainOuterRect.Width, _titleBarHeight);

            ImportTextures();

            if (_mainOuterRect.Width == 0)
                _mainOuterRect.Width = 10;
            if (_mainOuterRect.Height == 0)
                _mainOuterRect.Height = 10;

            renderTargetOuter = new RenderTarget2D(_gameMgr.GraphicsDevice, _mainOuterRect.Width, _mainOuterRect.Height, false, _gameMgr.GraphicsDevice.PresentationParameters.BackBufferFormat, 
                                                _gameMgr.GraphicsDevice.PresentationParameters.DepthStencilFormat, _gameMgr.GraphicsDevice.PresentationParameters.MultiSampleCount,  RenderTargetUsage.PreserveContents);
            renderTargetInner = new RenderTarget2D(_gameMgr.GraphicsDevice, _mainInnerRect.Width, _mainInnerRect.Height, false, _gameMgr.GraphicsDevice.PresentationParameters.BackBufferFormat, 
                                                _gameMgr.GraphicsDevice.PresentationParameters.DepthStencilFormat, _gameMgr.GraphicsDevice.PresentationParameters.MultiSampleCount,  RenderTargetUsage.PreserveContents);
            renderTargetTitleBar = new RenderTarget2D(_gameMgr.GraphicsDevice, _mainTitleBarRect.Width, _mainTitleBarRect.Height, false, _gameMgr.GraphicsDevice.PresentationParameters.BackBufferFormat, 
                                                _gameMgr.GraphicsDevice.PresentationParameters.DepthStencilFormat, _gameMgr.GraphicsDevice.PresentationParameters.MultiSampleCount,  RenderTargetUsage.PreserveContents);
        }

        private void _updateRenderTarget()
        {
            renderTargetOuter = new RenderTarget2D(_gameMgr.GraphicsDevice, _mainOuterRect.Width, _mainOuterRect.Height, false, _gameMgr.GraphicsDevice.PresentationParameters.BackBufferFormat, 
                                                _gameMgr.GraphicsDevice.PresentationParameters.DepthStencilFormat, _gameMgr.GraphicsDevice.PresentationParameters.MultiSampleCount,  RenderTargetUsage.PreserveContents);
            renderTargetInner = new RenderTarget2D(_gameMgr.GraphicsDevice, _mainInnerRect.Width, _mainInnerRect.Height, false, _gameMgr.GraphicsDevice.PresentationParameters.BackBufferFormat, 
                                                _gameMgr.GraphicsDevice.PresentationParameters.DepthStencilFormat, _gameMgr.GraphicsDevice.PresentationParameters.MultiSampleCount,  RenderTargetUsage.PreserveContents);
            renderTargetTitleBar = new RenderTarget2D(_gameMgr.GraphicsDevice, _mainTitleBarRect.Width, _mainTitleBarRect.Height, false, _gameMgr.GraphicsDevice.PresentationParameters.BackBufferFormat, 
                                                _gameMgr.GraphicsDevice.PresentationParameters.DepthStencilFormat, _gameMgr.GraphicsDevice.PresentationParameters.MultiSampleCount,  RenderTargetUsage.PreserveContents);
        }

        public void SetRenderTarget(RenderTarget2D target)
        {
            renderTarget = target;
            _gameMgr.SetRenderTarget(target);
        }

        public int BorderWidth
        {
            get
            {
                return _mainBorderWidth;
            }

            set
            {
                _mainBorderWidth = value;
                Width = Width;
                Height = Height;
                _setAbsXY();
            }
        }

        public Color BaseColor
        {
            get
            {
                return _baseColor;
            }

            set
            {
                _baseColor = value;
            }
        }

        public Color TitleBarColor
        {
            get
            {
                return _titleBarColor;
            }

            set
            {
                _titleBarColor = value;
            }
        }

        public Color BorderColor
        {
            get
            {
                return _borderColor;
            }

            set
            {
                _borderColor = value;
            }
        }

        public GameMgr GameMgr
        {
            get
            {
                return _gameMgr;
            }
        }

        public bool TitleBar
        {
            set
            {
                _titleBar = value;
                if (!_titleBar)
                {
                    Draggable = false;
                }
            }
        }

        public int X
        {
            get
            {
                return _mainRect.X;
            }

            set
            {
                _mainRect.X = value;
                _setAbsXY();
            }
        }

        public int Y
        {
            get
            {
                return _mainRect.Y;
            }

            set
            {
                _mainRect.Y = value;
                _setAbsXY();
            }
        }

        public int InnerX
        {
            get
            {
                return _mainInnerRect.X;
            }
        }

        public int InnerY
        {
            get
            {
                return _mainInnerRect.Y;
            }
        }

        public int InnerWidth
        {
            get
            {
                return _mainInnerRect.Width;
            }
        }

        public int InnerHeight
        {
            get
            {
                return _mainInnerRect.Height;
            }
        }

        public int Width
        {
            get
            {
                return _mainOuterRect.Width;
            }

            set
            {
                _mainOuterRect.Width = value;
                _mainRect.Width = value;
                _mainInnerRect.Width = value - 2 * _mainBorderWidth;
                _mainTitleBarRect.Width = value - 2 * _mainBorderWidth;

                _updateBorderRects();
                _updateTitleBarRect();

                _updateRenderTarget();
            }
        }

        public int Height
        {
            get
            {
                return _mainOuterRect.Height;
            }

            set
            {
                _mainOuterRect.Height = value;
                _mainRect.Height = value;
                _mainInnerRect.Height = value - 2 * _mainBorderWidth - _titleBarHeight;
                _mainTitleBarRect.Height = value - 2 * _mainBorderWidth;

                _updateBorderRects();
                _updateTitleBarRect();

                _updateRenderTarget();
            }
        }

        public SpriteFont Font
        {
            get
            {
                return _font;
            }

            set
            {
                _font = value;
                _fontHeight = (int)_font.MeasureString("A").Y;
                if (_titleBar)
                    _titleBarHeight = _fontHeight + 10;
            }
        }

        public void SetFont(string fontName)
        {
            SpriteFont temp = _gameMgr.Content.Load<SpriteFont>(fontName);

            if (temp != null)
            {
                Font = temp;
            }
        }

        public void ResetFont()
        {
            _font = _defaultFont;
        }


        public virtual void ImportTextures()
        {
            _baseTexture = _gameMgr.Content.Load<Texture2D>("WhiteBlock");
        }

        public void DrawBase()
        {

            _gameMgr.DrawSprite(_baseTexture, _mainOuterRect, _borderColor);

            _gameMgr.SpriteBatch.End();

            SetRenderTarget(renderTargetTitleBar);

            _gameMgr.SpriteBatch.Begin();

            if (_titleBar)
            {
                _gameMgr.DrawSprite(_baseTexture, _titleBarRect, _titleBarColor);
                _gameMgr.DrawString(_font, _title, new Vector2(10, (_titleBarHeight - _fontHeight) / 2), Color.Black, 1.0f);
            }

            _gameMgr.SpriteBatch.End();

            SetRenderTarget(renderTargetInner);

            _gameMgr.SpriteBatch.Begin();

            _gameMgr.DrawSprite(_baseTexture, _mainInnerRect, _baseColor);
        }

        public virtual void DrawMisc(GameTime gameTime)
        { }

        public virtual void UpdateMisc(GameTime gameTime)
        { }

        public override void Draw(GameTime gameTime)
        {
            num = 20;

            _gameMgr.SpriteBatch.End();
            SetRenderTarget(renderTargetOuter);
            _gameMgr.SpriteBatch.Begin();

            DrawBase();

            foreach (XNALabel label in _labels)
            {
                label.Draw();
            }

            foreach (Button button in _buttons)
            {
                button.Draw();
            }

            foreach (Clickable click in _clickables)
            {
                click.Draw();
            }

            foreach (TextField field in _textFields)
            {
                field.Draw();
            }

            DrawMisc(gameTime);

            // draws any other forms that may be held by current form
            base.Draw(gameTime);
            _gameMgr.SpriteBatch.End();

            SetRenderTarget(renderTargetTitleBar);

            _gameMgr.SpriteBatch.Begin();
            _gameMgr.DrawSprite(renderTargetInner, new Rectangle(0, _titleBarHeight, _mainInnerRect.Width, _mainInnerRect.Height), Color.White);
            _gameMgr.SpriteBatch.End();

            SetRenderTarget(renderTargetOuter);

            _gameMgr.SpriteBatch.Begin();
            _gameMgr.DrawSprite(renderTargetTitleBar, new Rectangle(_mainBorderWidth, _mainBorderWidth, _mainTitleBarRect.Width, _mainTitleBarRect.Height), Color.White);
            _gameMgr.SpriteBatch.End();

            _restoreRenderTarget();

            _gameMgr.SpriteBatch.Begin();

            _gameMgr.DrawSprite(renderTargetOuter, _mainRect, Color.White);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            foreach (Button button in _buttons)
            {
                button.Update();
            }

            foreach (Clickable click in _clickables)
            {
                click.Update();
            }

            foreach (TextField field in _textFields)
            {
                field.Update();
            }

        }

        private void _restoreRenderTarget()
        {
            SetRenderTarget(_parent.RenderTarget);
        }

    }
}
