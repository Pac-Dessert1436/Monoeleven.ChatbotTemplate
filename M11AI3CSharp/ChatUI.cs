using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace M11AI3CSharp;

public sealed class ChatUI : IDisposable
{
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D _userIcon = null!;
    private Texture2D _aiIcon = null!;
    private bool _isDisposed = false;

    private readonly List<ChatMessage> _messages = [];
    private string _inputText = string.Empty;
    private bool _inputActive = true;
    private bool _waitingForBotReply = false;
    private float _botReplyTimer = 0f;
    private string _pendingBotReply = string.Empty;
    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;

    // Scrolling variables
    private int _scrollOffset = 0;
    private int _maxScroll = 0;
    private Rectangle _scrollBarRect;
    private bool _isDraggingScrollBar = false;
    private Vector2 _lastMouseDragPosition;
    private RenderTarget2D _chatboxRenderTarget = null!;

    private const int ChatBoxMargin = 10;
    private const int InputBoxHeight = 50;
    private const int SendButtonWidth = 80;
    private const int LineSpacing = 5;
    private const int MaxLineWidth = 500;
    private const int MessagePadding = 10;
    private const int BubblePadding = 15;
    private const int IconSize = 32;
    private const int ScrollBarWidth = 10;
    private const int ScrollBarMinHeight = 20;

    public ChatUI(SpriteFont font, GraphicsDevice graphicsDevice)
    {
        _font = font;
        _graphicsDevice = graphicsDevice;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        _previousKeyboardState = Keyboard.GetState();
        _previousMouseState = Mouse.GetState();

        AddMessage("Mono11", "Hello! I am Monoeleven AI. How can I help you?");
    }

    public void LoadIcons(Texture2D userIcon, Texture2D aiIcon)
    {
        _userIcon = userIcon;
        _aiIcon = aiIcon;
    }

    public void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        if (_inputActive && !_waitingForBotReply)
        {
            HandleTextInput(keyboardState);
            HandleMouseInput(mouseState);
        }

        if (_waitingForBotReply)
        {
            _botReplyTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_botReplyTimer >= 1.0f)
            {
                AddMessage("Mono11", _pendingBotReply);
                _waitingForBotReply = false;
                _botReplyTimer = 0f;
                _pendingBotReply = string.Empty;
                _inputActive = true;
            }
        }

        _previousKeyboardState = keyboardState;
        _previousMouseState = mouseState;
    }

    private void HandleTextInput(KeyboardState keyboardState)
    {
        foreach (var key in keyboardState.GetPressedKeys())
        {
            if (!_previousKeyboardState.IsKeyDown(key))
            {
                if (key == Keys.Enter && _inputText.Length > 0)
                {
                    SendMessage();
                    return;
                }

                if (key == Keys.Back && _inputText.Length > 0)
                {
                    _inputText = _inputText[..^1];
                    continue;
                }

                string character = GetCharacterFromKey(key, keyboardState);
                if (!Equals(character, null) && _inputText.Length < 100)
                {
                    _inputText += character;
                }
            }
        }
    }

    private static string GetCharacterFromKey(Keys key, KeyboardState keyboardState)
    {
        bool shiftPressed = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);

        return key switch
        {
            Keys.Space => " ",
            Keys.A => shiftPressed ? "A" : "a",
            Keys.B => shiftPressed ? "B" : "b",
            Keys.C => shiftPressed ? "C" : "c",
            Keys.D => shiftPressed ? "D" : "d",
            Keys.E => shiftPressed ? "E" : "e",
            Keys.F => shiftPressed ? "F" : "f",
            Keys.G => shiftPressed ? "G" : "g",
            Keys.H => shiftPressed ? "H" : "h",
            Keys.I => shiftPressed ? "I" : "i",
            Keys.J => shiftPressed ? "J" : "j",
            Keys.K => shiftPressed ? "K" : "k",
            Keys.L => shiftPressed ? "L" : "l",
            Keys.M => shiftPressed ? "M" : "m",
            Keys.N => shiftPressed ? "N" : "n",
            Keys.O => shiftPressed ? "O" : "o",
            Keys.P => shiftPressed ? "P" : "p",
            Keys.Q => shiftPressed ? "Q" : "q",
            Keys.R => shiftPressed ? "R" : "r",
            Keys.S => shiftPressed ? "S" : "s",
            Keys.T => shiftPressed ? "T" : "t",
            Keys.U => shiftPressed ? "U" : "u",
            Keys.V => shiftPressed ? "V" : "v",
            Keys.W => shiftPressed ? "W" : "w",
            Keys.X => shiftPressed ? "X" : "x",
            Keys.Y => shiftPressed ? "Y" : "y",
            Keys.Z => shiftPressed ? "Z" : "z",
            Keys.D0 => shiftPressed ? ")" : "0",
            Keys.D1 => shiftPressed ? "!" : "1",
            Keys.D2 => shiftPressed ? "@" : "2",
            Keys.D3 => shiftPressed ? "#" : "3",
            Keys.D4 => shiftPressed ? "$" : "4",
            Keys.D5 => shiftPressed ? "%" : "5",
            Keys.D6 => shiftPressed ? "^" : "6",
            Keys.D7 => shiftPressed ? "&" : "7",
            Keys.D8 => shiftPressed ? "*" : "8",
            Keys.D9 => shiftPressed ? "(" : "9",
            Keys.OemComma => shiftPressed ? "<" : ",",
            Keys.OemPeriod => shiftPressed ? ">" : ".",
            Keys.OemQuestion => shiftPressed ? "?" : "/",
            Keys.OemSemicolon => shiftPressed ? ":" : ";",
            Keys.OemQuotes => shiftPressed ? "\"" : "'",
            Keys.OemTilde => shiftPressed ? "~" : "`",
            _ => default!
        };
    }

    private List<string> WrapText(string text, float maxWidth)
    {
        string[] words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = new System.Text.StringBuilder();
        float currentLineWidth = 0f;

        foreach (var word in words)
        {
            float wordWidth = _font.MeasureString(word).X;
            float spaceWidth = currentLine.Length > 0 ? _font.MeasureString(" ").X : 0f;

            if (currentLine.Length == 0)
            {
                currentLine.Append(word);
                currentLineWidth = wordWidth;
            }
            else if (currentLineWidth + spaceWidth + wordWidth <= maxWidth)
            {
                currentLine.Append(" " + word);
                currentLineWidth += spaceWidth + wordWidth;
            }
            else
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
                currentLine.Append(word);
                currentLineWidth = wordWidth;
            }
        }
        if (currentLine.Length > 0)
            lines.Add(currentLine.ToString());
        if (lines.Count == 0)
            lines.Add(text);
        return lines;
    }

    private void HandleMouseInput(MouseState mouseState)
    {
        var inputBoxRect = new Rectangle(
            ChatBoxMargin, 
            _graphicsDevice.Viewport.Height - InputBoxHeight - ChatBoxMargin, 
            _graphicsDevice.Viewport.Width - ChatBoxMargin * 2, InputBoxHeight
        );
        var sendButtonRect = new Rectangle(
            inputBoxRect.Right - SendButtonWidth - 5, inputBoxRect.Y + 5, 
            SendButtonWidth, inputBoxRect.Height - 10);

        // Handle send button click
        if (mouseState.LeftButton == ButtonState.Pressed && 
            _previousMouseState.LeftButton == ButtonState.Released && 
            sendButtonRect.Contains(mouseState.Position) && _inputText.Length > 0)
            SendMessage();

        // Handle scrollbar interaction
        var chatBoxRect = new Rectangle(
            ChatBoxMargin, ChatBoxMargin, 
            _graphicsDevice.Viewport.Width - ChatBoxMargin * 2, 
            _graphicsDevice.Viewport.Height - InputBoxHeight - ChatBoxMargin * 3);

        if (mouseState.LeftButton == ButtonState.Pressed && 
            _previousMouseState.LeftButton == ButtonState.Released && 
            _scrollBarRect.Contains(mouseState.Position) && _maxScroll > 0)
        {
            // Start dragging scrollbar
            _isDraggingScrollBar = true;
            _lastMouseDragPosition = new Vector2(mouseState.X, mouseState.Y);
        }
        else if (mouseState.LeftButton == ButtonState.Released)
        {
            // Stop dragging scrollbar
            _isDraggingScrollBar = false;
        }
        else if (_isDraggingScrollBar)
        {
            // Continue dragging scrollbar
            float deltaY = mouseState.Y - _lastMouseDragPosition.Y;
            _lastMouseDragPosition = new Vector2(mouseState.X, mouseState.Y);

            // Calculate how much to scroll based on drag distance
            float scrollRatio = (chatBoxRect.Height - 20 - _scrollBarRect.Height) / (float)_maxScroll;
            _scrollOffset = MathHelper.Clamp(
                _scrollOffset + (int)Math.Round(deltaY / scrollRatio), 0, _maxScroll
            );
        }

        // Handle mouse wheel scrolling
        int mouseWheelDelta = mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
        if (mouseWheelDelta != 0 && chatBoxRect.Contains(new Point(mouseState.X, mouseState.Y)))
        {
            int scrollAmount = -Math.Sign(mouseWheelDelta) * 30; // Adjust sensitivity
            _scrollOffset = MathHelper.Clamp(_scrollOffset + scrollAmount, 0, _maxScroll);
        }
    }

    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_inputText))
            return;

        AddMessage("User", _inputText);
        _pendingBotReply = ChatBotLogic.GetChatbotReply(_inputText);
        _waitingForBotReply = true;
        _inputActive = false;
        _inputText = string.Empty;
    }

    private void AddMessage(string sender, string text)
    {
        _messages.Add(new ChatMessage() { Sender = sender, Text = text });
    }

    public void Draw(SpriteBatch spriteBatch, int screenWidth, int screenHeight, Point positionOffset = default)
    {
        var chatBoxRect = new Rectangle(ChatBoxMargin + positionOffset.X, ChatBoxMargin + positionOffset.Y, screenWidth - ChatBoxMargin * 2, screenHeight - InputBoxHeight - ChatBoxMargin * 3);
        var inputBoxRect = new Rectangle(ChatBoxMargin + positionOffset.X, positionOffset.Y + screenHeight - InputBoxHeight - ChatBoxMargin, screenWidth - ChatBoxMargin * 2, InputBoxHeight);
        var sendButtonRect = new Rectangle(inputBoxRect.Right - SendButtonWidth - 5, inputBoxRect.Y + 5, SendButtonWidth, inputBoxRect.Height - 10);

        DrawChatBox(spriteBatch, chatBoxRect);
        DrawInputBox(spriteBatch, inputBoxRect, sendButtonRect);
    }

    // TODO: Introduce RenderTarget2D for chat box area
    private void DrawChatBox(SpriteBatch spriteBatch, Rectangle rect)
    {
        // Initialize or resize render target if needed
        if (_chatboxRenderTarget is null || 
            _chatboxRenderTarget.Width != rect.Width || _chatboxRenderTarget.Height != rect.Height)
        {
            _chatboxRenderTarget?.Dispose();
            _chatboxRenderTarget = new RenderTarget2D(_graphicsDevice, rect.Width, rect.Height);
        }

        // Set render target
        _graphicsDevice.SetRenderTarget(_chatboxRenderTarget);
        _graphicsDevice.Clear(Color.WhiteSmoke);

        // Draw content to render target
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
        // Draw top border
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, rect.Width, 2), Color.Gray);

        // Draw messages
        int yPos = 10 - _scrollOffset;
        foreach (var message in _messages)
        {
            var wrappedLines = WrapText(message.Text, MaxLineWidth);
            float maxLineWidth = 0f;
            foreach (var line in wrappedLines)
            {
                float lineWidth = _font.MeasureString(line).X;
                if (lineWidth > maxLineWidth)
                    maxLineWidth = lineWidth;
            }

            int totalTextHeight = wrappedLines.Count * _font.LineSpacing + (wrappedLines.Count - 1) * LineSpacing;
            int bubbleHeight = Math.Max(totalTextHeight + BubblePadding * 2, IconSize + BubblePadding * 2);
            float bubbleWidth = maxLineWidth + BubblePadding * 2 + IconSize + MessagePadding;

            bool isUserMessage = ReferenceEquals(message.Sender, "User");
            float bubbleX = isUserMessage ? rect.Width - bubbleWidth - 20f : 10f;

            var bubbleRect = new Rectangle((int)Math.Round(MathF.Round(bubbleX)), yPos, (int)Math.Round(bubbleWidth), bubbleHeight);
            var bubbleColor = isUserMessage ? new Color(220, 248, 198) : new Color(230, 230, 250);
            spriteBatch.Draw(_pixel, bubbleRect, bubbleColor);

            var iconTexture = isUserMessage ? _userIcon : _aiIcon;
            if (iconTexture is not null)
            {
                var iconRect = new Rectangle(isUserMessage ? bubbleRect.Right - IconSize - 5 : bubbleRect.X + 5, (int)Math.Round(bubbleRect.Y + (bubbleHeight - IconSize) / 2d), IconSize, IconSize);
                spriteBatch.Draw(iconTexture, iconRect, Color.White);
            }

            double textStartY = bubbleRect.Y + (bubbleHeight - totalTextHeight) / 2d;
            for (int i = 0, loopTo = wrappedLines.Count - 1; i <= loopTo; i++)
            {
                string lineText = wrappedLines[i];
                var textSize = _font.MeasureString(lineText);
                var textPosition = new Vector2(isUserMessage ? bubbleRect.Right - textSize.X - IconSize - MessagePadding : bubbleRect.X + IconSize + MessagePadding, (float)(textStartY + i * (_font.LineSpacing + LineSpacing)));
                spriteBatch.DrawString(_font, lineText, textPosition, Color.Black);
            }

            yPos += bubbleHeight + MessagePadding;
        }

        // Calculate scroll values
        float totalMessagesHeight = 0f;
        foreach (var message in _messages)
        {
            var wrappedLines = WrapText(message.Text, MaxLineWidth);
            float maxLineWidth = 0f;
            foreach (var line in wrappedLines)
            {
                float lineWidth = _font.MeasureString(line).X;
                if (lineWidth > maxLineWidth)
                {
                    maxLineWidth = lineWidth;
                }
            }

            int totalTextHeight = wrappedLines.Count * _font.LineSpacing + (wrappedLines.Count - 1) * LineSpacing;
            int bubbleHeight = Math.Max(totalTextHeight + BubblePadding * 2, IconSize + BubblePadding * 2);
            totalMessagesHeight += bubbleHeight + MessagePadding;
        }

        _maxScroll = Math.Max(0, (int)Math.Round(totalMessagesHeight - rect.Height + 20f));

        int scrollBarHeight = Math.Max(ScrollBarMinHeight, (int)Math.Round(rect.Height / Math.Max(totalMessagesHeight, rect.Height) * rect.Height));
        int scrollBarY = 10 + (int)Math.Round(_scrollOffset / (float)Math.Max(_maxScroll, 1) * (rect.Height - 10 - scrollBarHeight));

        _scrollBarRect = new Rectangle(rect.Width - ScrollBarWidth - 5, scrollBarY, ScrollBarWidth, scrollBarHeight);

        // Draw scrollbar track
        spriteBatch.Draw(_pixel, new Rectangle(_scrollBarRect.X - 2, 10, ScrollBarWidth + 4, rect.Height - 20), Color.LightGray);

        // Draw scrollbar thumb
        spriteBatch.Draw(_pixel, _scrollBarRect, _isDraggingScrollBar ? Color.DarkGray : Color.Gray);
        spriteBatch.End();

        // Reset render target and draw the chatbox to the screen
        _graphicsDevice.SetRenderTarget(null);
        _graphicsDevice.Clear(Color.AntiqueWhite);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
        spriteBatch.Draw(_chatboxRenderTarget, new Vector2(rect.X, rect.Y), Color.White);
        // NOTE: Do not call `spriteBatch.End()` here; the SpriteBatch continues drawing graphics
        //       on the `GameMain.Draw()` call.
    }

    private void DrawInputBox(SpriteBatch spriteBatch, Rectangle inputRect, Rectangle sendButtonRect)
    {
        spriteBatch.Draw(_pixel, inputRect, Color.White);
        spriteBatch.Draw(_pixel, new Rectangle(inputRect.X, inputRect.Y, inputRect.Width, 2), Color.Gray);

        var inputTextPos = new Vector2(inputRect.X + 10, inputRect.Y + 15);
        spriteBatch.DrawString(_font, _inputText + (_inputActive ? "|" : ""), inputTextPos, Color.Black);

        spriteBatch.Draw(_pixel, sendButtonRect, Color.LightBlue);
        spriteBatch.Draw(_pixel, new Rectangle(sendButtonRect.X, sendButtonRect.Y, sendButtonRect.Width, 2), Color.Blue);

        string buttonText = "Send";
        var buttonTextSize = _font.MeasureString(buttonText);
        var buttonTextPos = new Vector2(sendButtonRect.X + (sendButtonRect.Width - buttonTextSize.X) / 2f, sendButtonRect.Y + (sendButtonRect.Height - buttonTextSize.Y) / 2f);
        spriteBatch.DrawString(_font, buttonText, buttonTextPos, Color.Black);
    }

    private class ChatMessage
    {
        public string Sender { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _pixel.Dispose();
                _chatboxRenderTarget?.Dispose();
            }
            _isDisposed = true;
        }
    }
}