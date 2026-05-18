Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Public NotInheritable Class GameMain
    Inherits Game

    Private ReadOnly _graphics As GraphicsDeviceManager
    Private _spriteBatch As SpriteBatch
    Private _font As SpriteFont
    Private _chatUI As ChatUI
    Private _titleImage As Texture2D

    Private Const ScreenWidth As Integer = 800
    Private Const ScreenHeight As Integer = 600

    Public Sub New()
        _graphics = New GraphicsDeviceManager(Me)
        Content.RootDirectory = "Content"
        IsMouseVisible = True
        Window.Title = "Monoeleven AI 3.0"

        _graphics.PreferredBackBufferWidth = ScreenWidth
        _graphics.PreferredBackBufferHeight = ScreenHeight
    End Sub

    Protected Overrides Sub Initialize()
        MyBase.Initialize()
    End Sub

    Protected Overrides Sub LoadContent()
        _spriteBatch = New SpriteBatch(GraphicsDevice)
        _font = Content.Load(Of SpriteFont)("Fonts/ChatFontEN")
        _chatUI = New ChatUI(_font, GraphicsDevice)
        _titleImage = Content.Load(Of Texture2D)("Images/monoeleven_title")

        ' Load user and AI icons
        Dim userIcon = Content.Load(Of Texture2D)("Images/user_icon")
        Dim aiIcon = Content.Load(Of Texture2D)("Images/m11_icon")

        _chatUI.LoadIcons(userIcon, aiIcon)
    End Sub

    Protected Overrides Sub Update(gameTime As GameTime)
        If GamePad.GetState(PlayerIndex.One).Buttons.Back = ButtonState.Pressed OrElse
            Keyboard.GetState().IsKeyDown(Keys.Escape) Then [Exit]()

        _chatUI.Update(gameTime)
        MyBase.Update(gameTime)
    End Sub

    Protected Overrides Sub Draw(gameTime As GameTime)
        ' Draw chat UI below the title (offset Y by 80 for title)
        ' `_spriteBatch.Begin()` is already called after resetting RenderTarget2D inside chat UI.
        _chatUI.Draw(_spriteBatch, ScreenWidth, ScreenHeight - 80, New Point(0, 80))
        ' Draw title image at the top (top portion for title)
        Dim titleRect As New Rectangle(0, 0, _titleImage.Width, _titleImage.Height)
        _spriteBatch.Draw(_titleImage, titleRect, Color.White)
        _spriteBatch.End()

        MyBase.Draw(gameTime)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _spriteBatch.Dispose()
            _chatUI.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    Friend Shared Sub Main()
        Using game As New GameMain
            game.Run()
        End Using
    End Sub
End Class