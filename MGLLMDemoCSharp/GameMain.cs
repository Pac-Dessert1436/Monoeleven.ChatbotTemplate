namespace MGLLMDemoCSharp;

public class GameMain : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _font = null!;
    private ChatUI _chatUI = null!;
    private Texture2D _titleImage = null!;

    private const int ScreenWidth = 800;
    private const int ScreenHeight = 600;

    public GameMain()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        _graphics.PreferredBackBufferWidth = ScreenWidth;
        _graphics.PreferredBackBufferHeight = ScreenHeight;
    }

    protected override void Initialize()
    {
        ChatBotLogic.LoadAppConfig();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("Fonts/ChatFontEN");
        _chatUI = new ChatUI(_font, GraphicsDevice);
        _titleImage = Content.Load<Texture2D>("Images/title_card");

        // Load user and AI icons
        Texture2D userIcon = Content.Load<Texture2D>("Images/user_icon");
        Texture2D aiIcon = Content.Load<Texture2D>("Images/mgllm_icon");
        
        _chatUI.LoadIcons(userIcon, aiIcon);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
            Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();

        _chatUI.Update(ScreenHeight);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Draw chat UI below the title (offset Y by 80 for title)
        // `_spriteBatch.Begin()` is already called after resetting RenderTarget2D inside chat UI.
        _chatUI.Draw(_spriteBatch, ScreenWidth, ScreenHeight - 80, new Point(0, 80));
        // Draw title image at the top (top portion for title)
        Rectangle titleRect = new(0, 0, _titleImage.Width, _titleImage.Height);
        _spriteBatch.Draw(_titleImage, titleRect, Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _spriteBatch.Dispose();
            _chatUI.Dispose();
        }
        base.Dispose(disposing);
    }

    internal static void Main()
    {
        using GameMain game = new();
        game.Run();
    }
}