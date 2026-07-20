using System.Text;
using System.Text.RegularExpressions;

namespace MGLLMDemoCSharp;

public sealed partial class ChatUI : IDisposable
{

    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D _userIcon = null!;
    private Texture2D _aiIcon = null!;
    private bool _isDisposed = false;

    private readonly List<ChatMessage> _messages = [];
    private readonly List<Message> _conversationHistory = [];
    private string _inputText = string.Empty;
    private bool _inputActive = true;
    private int _caretIndex = 0;
    private float _inputScrollX = 0f;
    private bool _waitingForBotReply = false;
    private string _pendingBotReply = string.Empty;
    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;
    private Task _currentReplyTask = null!;
    private CancellationTokenSource _cancellationTokenSource = null!;

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
    private const int HScrollBarHeight = 6;

    public ChatUI(SpriteFont font, GraphicsDevice graphicsDevice)
    {
        _font = font;
        _graphicsDevice = graphicsDevice;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        _previousKeyboardState = Keyboard.GetState();
        _previousMouseState = Mouse.GetState();

        AddMessage("MGLLM", "Hello! I am MonoGame LLM. How can I help you?");
    }

    // Add method to clear conversation history
    public void ClearConversation()
    {
        _messages.Clear();
        _conversationHistory.Clear();
        _scrollOffset = 0;
        _maxScroll = 0;
        AddMessage("MGLLM", "Hello! I am MonoGame LLM. How can I help you?");
    }

    public void LoadIcons(Texture2D userIcon, Texture2D aiIcon)
    {
        _userIcon = userIcon;
        _aiIcon = aiIcon;
    }

    public void Update(int screenHeight)
    {
        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        if (_inputActive && !_waitingForBotReply)
        {
            HandleTextInput(keyboardState);
            HandleMouseInput(mouseState, screenHeight);
        }

        if (_waitingForBotReply && !string.IsNullOrEmpty(_pendingBotReply))
        {
            AddMessage("MGLLM", _pendingBotReply);
            _pendingBotReply = string.Empty;
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
                // Submit on Enter
                if (key == Keys.Enter && _inputText.Length > 0)
                {
                    SendMessage();
                    return;
                }

                // Backspace: remove character before caret
                if (key == Keys.Back)
                {
                    if (_caretIndex > 0 && _inputText.Length > 0)
                    {
                        _inputText = _inputText.Remove(_caretIndex - 1, 1);
                        _caretIndex = Math.Max(0, _caretIndex - 1);
                    }
                    continue;
                }

                // Delete: remove character at caret
                if (key == Keys.Delete)
                {
                    if (_caretIndex < _inputText.Length)
                    {
                        _inputText = _inputText.Remove(_caretIndex, 1);
                    }
                    continue;
                }

                // Move caret left/right/home/end
                if (key == Keys.Left)
                {
                    _caretIndex = Math.Max(0, _caretIndex - 1);
                    continue;
                }
                else if (key == Keys.Right)
                {
                    _caretIndex = Math.Min(_inputText.Length, _caretIndex + 1);
                    continue;
                }
                else if (key == Keys.Home)
                {
                    _caretIndex = 0;
                    continue;
                }
                else if (key == Keys.End)
                {
                    _caretIndex = _inputText.Length;
                    continue;
                }

                // Handle Ctrl+L to clear conversation
                if (key == Keys.L && (keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl)))
                {
                    ClearConversation();
                    continue;
                }

                string character = GetCharacterFromKey(key, keyboardState);
                if (!Equals(character, null))
                {
                    _inputText = _inputText.Insert(_caretIndex, character);
                    _caretIndex += 1;
                }
            }
        }
    }

    private static string GetCharacterFromKey(Keys key, KeyboardState keyboardState)
    {
        bool shiftPressed = keyboardState.IsKeyDown(Keys.LeftShift) || 
            keyboardState.IsKeyDown(Keys.RightShift);

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
            Keys.OemMinus => shiftPressed ? "_" : "-",
            Keys.OemPlus => shiftPressed ? "+" : "=",
            Keys.OemPipe => shiftPressed ? "|" : "\\",
            _ => null!,
        };
    }

    private List<string> WrapText(string text, float maxWidth)
    {
        // First clean up any markdown artifacts
        string cleanedText = text;
        // Remove markdown bold/italic markers
        cleanedText = BoldMarkdownRegex().Replace(cleanedText, "");
        // Remove extra whitespace
        cleanedText = WhitespaceRegex().Replace(cleanedText, " ").Trim();

        string[] words = cleanedText.Split(' ');
        List<string> lines = [];
        StringBuilder currentLine = new();
        float currentLineWidth = 0f;

        foreach (var word in words)
        {
            if (string.IsNullOrWhiteSpace(word))
                continue;

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
            lines.Add(cleanedText);
        return lines;
    }

    private void HandleMouseInput(MouseState mouseState, int screenHeight)
    {
        Rectangle inputBoxRect = new(ChatBoxMargin, screenHeight - InputBoxHeight - ChatBoxMargin, _graphicsDevice.Viewport.Width - ChatBoxMargin * 2, InputBoxHeight);
        Rectangle sendButtonRect = new(inputBoxRect.Right - SendButtonWidth - 5, inputBoxRect.Y + 5, SendButtonWidth, inputBoxRect.Height - 10);

        // Handle send button click
        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released && sendButtonRect.Contains(mouseState.Position) && _inputText.Length > 0)
            SendMessage();

        // Handle clicking inside input area to set caret
        Rectangle innerInputRect = new(inputBoxRect.X + 5, inputBoxRect.Y + 5, sendButtonRect.X - inputBoxRect.X - 15, inputBoxRect.Height - 10);
        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released && innerInputRect.Contains(mouseState.Position))
        {
            // Determine approximate caret index by measuring text width per character
            int clickX = mouseState.X - (innerInputRect.X + 10) + (int)Math.Round(_inputScrollX);
            float accum = 0f;
            int foundIndex = 0;
            for (int i = 0, loopTo = _inputText.Length - 1; i <= loopTo; i++)
            {
                float chWidth = _font.MeasureString(_inputText.Substring(i, 1)).X;
                if (accum + chWidth / 2f >= clickX)
                {
                    foundIndex = i;
                    break;
                }
                accum += chWidth;
                foundIndex = i + 1;
            }
            _caretIndex = Math.Max(0, Math.Min(_inputText.Length, foundIndex));
        }

        // Handle scrollbar interaction
        Rectangle chatBoxRect = new(ChatBoxMargin, ChatBoxMargin, _graphicsDevice.Viewport.Width - ChatBoxMargin * 2, screenHeight - InputBoxHeight - ChatBoxMargin * 3);

        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released && _scrollBarRect.Contains(mouseState.Position) && _maxScroll > 0)
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
            _scrollOffset = MathHelper.Clamp(_scrollOffset + (int)Math.Round(deltaY / scrollRatio), 0, _maxScroll);
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
        if (_waitingForBotReply)
            return;

        AddMessage("User", _inputText);
        _waitingForBotReply = true;
        _inputActive = false;
        _inputText = string.Empty;
        _caretIndex = 0;
        _inputScrollX = 0f;

        // Start async task to get bot reply
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        string userMessage = _messages.Last().Text;

        // Add to conversation history and save chat log
        _currentReplyTask = Task.Run(
            async () =>
            {
                try
                {
                    string reply = await ChatBotLogic.GetDeepSeekResponseAsync(userMessage, _conversationHistory);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        _pendingBotReply = reply;
                        _conversationHistory.Add(new Message() { Role = "user", Content = userMessage });
                        _conversationHistory.Add(new Message() { Role = "assistant", Content = reply });
                        ChatBotLogic.SaveChatLog(userMessage, reply, ChatBotLogic.NextConversationIndex);
                    }
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                        _pendingBotReply = "Error: " + ex.Message;
                }
                finally
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        _waitingForBotReply = false;
                        _inputActive = true;
                    }
                }
            }, cancellationToken);
    }

    private void AddMessage(string sender, string text)
    {
        _messages.Add(new ChatMessage() { Sender = sender, Text = text });
        // Calculate max scroll immediately after adding message
        CalculateMaxScroll();
        // Auto-scroll to bottom when new message is added
        _scrollOffset = _maxScroll;
    }

    private void CalculateMaxScroll()
    {
        float totalMessagesHeight = 0f;
        foreach (ChatMessage message in _messages)
        {
            var wrappedLines = WrapText(message.Text, MaxLineWidth);
            float maxLineWidth = 0f;
            foreach (string line in wrappedLines)
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

        int chatBoxHeight = _graphicsDevice.Viewport.Height - InputBoxHeight - ChatBoxMargin * 3;
        _maxScroll = Math.Max(0, (int)Math.Round(totalMessagesHeight - chatBoxHeight + 20f));
    }

    public void Draw(SpriteBatch spriteBatch, int screenWidth, int screenHeight, Point positionOffset = default)
    {
        Rectangle chatBoxRect = new(ChatBoxMargin + positionOffset.X, ChatBoxMargin + positionOffset.Y, screenWidth - ChatBoxMargin * 2, screenHeight - InputBoxHeight - ChatBoxMargin * 3);
        Rectangle inputBoxRect = new(ChatBoxMargin + positionOffset.X, positionOffset.Y + screenHeight - InputBoxHeight - ChatBoxMargin, screenWidth - ChatBoxMargin * 2, InputBoxHeight);
        Rectangle sendButtonRect = new(inputBoxRect.Right - SendButtonWidth - 5, inputBoxRect.Y + 5, SendButtonWidth, inputBoxRect.Height - 10);

        DrawChatBox(spriteBatch, chatBoxRect);
        DrawInputBox(spriteBatch, inputBoxRect, sendButtonRect);
    }

    // TODO: Introduce RenderTarget2D for chat box area
    private void DrawChatBox(SpriteBatch spriteBatch, Rectangle rect)
    {
        // Initialize or resize render target if needed
        if (_chatboxRenderTarget is null || _chatboxRenderTarget.Width != rect.Width || _chatboxRenderTarget.Height != rect.Height)
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

            Rectangle bubbleRect = new((int)Math.Round(MathF.Round(bubbleX)), yPos, (int)Math.Round(bubbleWidth), bubbleHeight);
            var bubbleColor = isUserMessage ? new Color(220, 248, 198) : new Color(230, 230, 250);
            spriteBatch.Draw(_pixel, bubbleRect, bubbleColor);

            var iconTexture = isUserMessage ? _userIcon : _aiIcon;
            if (iconTexture is not null)
            {
                Rectangle iconRect = new(isUserMessage ? bubbleRect.Right - IconSize - 5 : bubbleRect.X + 5, (int)Math.Round(bubbleRect.Y + (bubbleHeight - IconSize) / 2d), IconSize, IconSize);
                spriteBatch.Draw(iconTexture, iconRect, Color.White);
            }

            double textStartY = bubbleRect.Y + (bubbleHeight - totalTextHeight) / 2d;
            for (int i = 0, loopTo = wrappedLines.Count - 1; i <= loopTo; i++)
            {
                string lineText = wrappedLines[i];
                var textSize = _font.MeasureString(lineText);
                Vector2 textPosition = new(isUserMessage ? bubbleRect.Right - textSize.X - IconSize - MessagePadding : bubbleRect.X + IconSize + MessagePadding, (float)(textStartY + i * (_font.LineSpacing + LineSpacing)));
                spriteBatch.DrawString(_font, lineText, textPosition, Color.Black);
            }

            yPos += bubbleHeight + MessagePadding;
        }

        // Calculate scroll values
        CalculateMaxScroll();
        // Adjust maxScroll based on current chat box height
        _maxScroll = Math.Max(0, _maxScroll - (rect.Height - (_graphicsDevice.Viewport.Height - InputBoxHeight - ChatBoxMargin * 3)));

        int totalMessagesHeight = _maxScroll + rect.Height - 20;
        int scrollBarHeight = Math.Max(ScrollBarMinHeight, (int)Math.Round(rect.Height / (double)Math.Max(totalMessagesHeight, rect.Height) * rect.Height));
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
        // NOTE: Do not call spriteBatch.End() here; the SpriteBatch continues drawing graphics
        // on the GameMain.Draw() call.
    }

    private void DrawInputBox(SpriteBatch spriteBatch, Rectangle inputRect, Rectangle sendButtonRect)
    {
        // Draw full input background area
        spriteBatch.Draw(_pixel, inputRect, Color.White);
        spriteBatch.Draw(_pixel, new Rectangle(inputRect.X, inputRect.Y, inputRect.Width, 2), Color.Gray);

        // Inner input area (stop before the send button)
        Rectangle innerRect = new(inputRect.X + 5, inputRect.Y + 5, sendButtonRect.X - inputRect.X - 15, inputRect.Height - 10);
        spriteBatch.Draw(_pixel, innerRect, Color.White);

        // Padding for text inside input
        int leftPadding = 10;
        int rightPadding = 10;

        // Measure cumulative widths to support caret and scrolling
        var cumWidths = new float[_inputText.Length + 1];
        cumWidths[0] = 0f;
        for (int i = 1, loopTo = _inputText.Length; i <= loopTo; i++)
            cumWidths[i] = cumWidths[i - 1] + _font.MeasureString(_inputText.Substring(i - 1, 1)).X;

        float textTotalWidth = _inputText.Length == 0 ? 0f : cumWidths[_inputText.Length];

        // Ensure caret visibility by adjusting _inputScrollX
        float caretPixelPos = _caretIndex >= 0 && _caretIndex <= _inputText.Length ? cumWidths[_caretIndex] : 0f;
        int visibleWidth = innerRect.Width - leftPadding - rightPadding;
        if (caretPixelPos - _inputScrollX > visibleWidth)
        {
            _inputScrollX = caretPixelPos - visibleWidth;
        }
        else if (caretPixelPos - _inputScrollX < 0f)
        {
            _inputScrollX = caretPixelPos;
        }
        _inputScrollX = Math.Max(0f, Math.Min(_inputScrollX, Math.Max(0f, textTotalWidth - visibleWidth)));

        // Find visible substring range
        int startIndex = 0;
        for (int i = 0, loopTo1 = _inputText.Length; i <= loopTo1; i++)
        {
            if (cumWidths[i] >= _inputScrollX)
            {
                startIndex = i;
                break;
            }
        }
        int endIndex = startIndex;
        for (int i = startIndex, loopTo2 = _inputText.Length; i <= loopTo2; i++)
        {
            if (cumWidths[i] - _inputScrollX > visibleWidth)
            {
                endIndex = i;
                break;
            }
            endIndex = i;
        }
        endIndex = Math.Min(_inputText.Length, Math.Max(startIndex, endIndex));

        string visibleText = endIndex > startIndex ? _inputText[startIndex..endIndex] : string.Empty;
        float drawX = innerRect.X + leftPadding + (cumWidths[startIndex] - _inputScrollX);
        int drawY = innerRect.Y + (innerRect.Height - _font.LineSpacing) / 2;
        spriteBatch.DrawString(_font, visibleText, new Vector2(drawX, drawY), Color.Black);

        // Draw caret as a thin vertical line
        if (_inputActive)
        {
            float caretX = innerRect.X + leftPadding + (caretPixelPos - _inputScrollX);
            double caretY = innerRect.Y + (innerRect.Height - _font.LineSpacing) / 2d;
            Rectangle caretRect = new((int)Math.Round(Math.Round((double)caretX)), (int)Math.Round(caretY), 2, _font.LineSpacing);
            spriteBatch.Draw(_pixel, caretRect, Color.Black);
        }

        // Draw optional horizontal scrollbar when text is wider than visible area
        if (textTotalWidth > visibleWidth)
        {
            int hBarWidth = (int)Math.Round(visibleWidth / textTotalWidth * visibleWidth);
            hBarWidth = Math.Max(20, hBarWidth);
            int hBarX = innerRect.X + (int)Math.Round(_inputScrollX / Math.Max(1.0f, textTotalWidth - visibleWidth) * (visibleWidth - hBarWidth));
            int hBarY = innerRect.Bottom - HScrollBarHeight - 4;
            spriteBatch.Draw(_pixel, new Rectangle(innerRect.X, hBarY, innerRect.Width, HScrollBarHeight), Color.LightGray);
            spriteBatch.Draw(_pixel, new Rectangle(hBarX, hBarY, hBarWidth, HScrollBarHeight), Color.Gray);
        }

        // Draw send button
        spriteBatch.Draw(_pixel, sendButtonRect, Color.LightBlue);
        spriteBatch.Draw(_pixel, new Rectangle(sendButtonRect.X, sendButtonRect.Y, sendButtonRect.Width, 2), Color.Blue);

        string buttonText = "Send";
        var buttonTextSize = _font.MeasureString(buttonText);
        Vector2 buttonTextPos = new(sendButtonRect.X + (sendButtonRect.Width - buttonTextSize.X) / 2f, sendButtonRect.Y + (sendButtonRect.Height - buttonTextSize.Y) / 2f);
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
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                if (_currentReplyTask is not null && !_currentReplyTask.IsCompleted)
                {
                    try
                    {
                        _currentReplyTask.Wait(1000);
                    }
                    catch
                    {
                        // Ignore cancellation exceptions
                    }
                }
            }
            _isDisposed = true;
        }
    }

    [GeneratedRegex(@"\*\*|\*|__|_")]
    private static partial Regex BoldMarkdownRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}