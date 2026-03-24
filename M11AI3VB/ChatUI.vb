Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Public NotInheritable Class ChatUI
    Implements IDisposable

    Private ReadOnly _font As SpriteFont
    Private ReadOnly _pixel As Texture2D
    Private ReadOnly _graphicsDevice As GraphicsDevice
    Private _userIcon As Texture2D
    Private _aiIcon As Texture2D
    Private _isDisposed As Boolean = False

    Private ReadOnly _messages As List(Of ChatMessage) = New List(Of ChatMessage)()
    Private _inputText As String = String.Empty
    Private _inputActive As Boolean = True
    Private _waitingForBotReply As Boolean = False
    Private _botReplyTimer As Single = 0F
    Private _pendingBotReply As String = String.Empty
    Private _previousKeyboardState As KeyboardState
    Private _previousMouseState As MouseState

    ' Scrolling variables
    Private _scrollOffset As Integer = 0
    Private _maxScroll As Integer = 0
    Private _scrollBarRect As Rectangle
    Private _isDraggingScrollBar As Boolean = False
    Private _lastMouseDragPosition As Vector2
    Private Const ChatBoxMargin As Integer = 10
    Private Const InputBoxHeight As Integer = 50
    Private Const SendButtonWidth As Integer = 80
    'Private Const LineSpacing As Integer = 5
    Private Const MessagePadding As Integer = 10
    Private Const BubblePadding As Integer = 15
    Private Const IconSize As Integer = 32
    Private Const ScrollBarWidth As Integer = 10
    Private Const ScrollBarMinHeight As Integer = 20

    Public Sub New(font As SpriteFont, graphicsDevice As GraphicsDevice)
        _font = font
        _graphicsDevice = graphicsDevice
        _pixel = New Texture2D(graphicsDevice, 1, 1)
        _pixel.SetData({Color.White})

        _previousKeyboardState = Keyboard.GetState()
        _previousMouseState = Mouse.GetState()

        AddMessage("Mono11", "Hello! I am Monoeleven AI. How can I help you?")
    End Sub

    Public Sub LoadIcons(userIcon As Texture2D, aiIcon As Texture2D)
        _userIcon = userIcon
        _aiIcon = aiIcon
    End Sub

    Public Sub Update(gameTime As GameTime)
        Dim keyboardState = Keyboard.GetState()
        Dim mouseState = Mouse.GetState()

        If _inputActive AndAlso Not _waitingForBotReply Then
            HandleTextInput(keyboardState)
            HandleMouseInput(mouseState)
        End If

        If _waitingForBotReply Then
            _botReplyTimer += CSng(gameTime.ElapsedGameTime.TotalSeconds)
            If _botReplyTimer >= 1.0F Then
                AddMessage("Mono11", _pendingBotReply)
                _waitingForBotReply = False
                _botReplyTimer = 0F
                _pendingBotReply = String.Empty
                _inputActive = True
            End If
        End If

        _previousKeyboardState = keyboardState
        _previousMouseState = mouseState
    End Sub

    Private Sub HandleTextInput(keyboardState As KeyboardState)
        For Each key In keyboardState.GetPressedKeys()
            If Not _previousKeyboardState.IsKeyDown(key) Then
                If key = Keys.Enter AndAlso _inputText.Length > 0 Then
                    SendMessage()
                    Return
                End If

                If key = Keys.Back AndAlso _inputText.Length > 0 Then
                    _inputText = _inputText.Substring(0, _inputText.Length - 1)
                    Continue For
                End If

                Dim character = GetCharacterFromKey(key, keyboardState)
                If Not Equals(character, Nothing) AndAlso _inputText.Length < 100 Then
                    _inputText &= character
                End If
            End If
        Next
    End Sub

    Private Shared Function GetCharacterFromKey(key As Keys, keyboardState As KeyboardState) As String
        Dim shiftPressed = keyboardState.IsKeyDown(Keys.LeftShift) OrElse keyboardState.IsKeyDown(Keys.RightShift)

        Select case key
            Case Keys.Space : Return " "
            Case Keys.A : Return If(shiftPressed, "A", "a")
            Case Keys.B : Return If(shiftPressed, "B", "b")
            Case Keys.C : Return If(shiftPressed, "C", "c")
            Case Keys.D : Return If(shiftPressed, "D", "d")
            Case Keys.E : Return If(shiftPressed, "E", "e")
            Case Keys.F : Return If(shiftPressed, "F", "f")
            Case Keys.G : Return If(shiftPressed, "G", "g")
            Case Keys.H : Return If(shiftPressed, "H", "h")
            Case Keys.I : Return If(shiftPressed, "I", "i")
            Case Keys.J : Return If(shiftPressed, "J", "j")
            Case Keys.K : Return If(shiftPressed, "K", "k")
            Case Keys.L : Return If(shiftPressed, "L", "l")
            Case Keys.M : Return If(shiftPressed, "M", "m")
            Case Keys.N : Return If(shiftPressed, "N", "n")
            Case Keys.O : Return If(shiftPressed, "O", "o")
            Case Keys.P : Return If(shiftPressed, "P", "p")
            Case Keys.Q : Return If(shiftPressed, "Q", "q")
            Case Keys.R : Return If(shiftPressed, "R", "r")
            Case Keys.S : Return If(shiftPressed, "S", "s")
            Case Keys.T : Return If(shiftPressed, "T", "t")
            Case Keys.U : Return If(shiftPressed, "U", "u")
            Case Keys.V : Return If(shiftPressed, "V", "v")
            Case Keys.W : Return If(shiftPressed, "W", "w")
            Case Keys.X : Return If(shiftPressed, "X", "x")
            Case Keys.Y : Return If(shiftPressed, "Y", "y")
            Case Keys.Z : Return If(shiftPressed, "Z", "z")
            Case Keys.D0 : Return If(shiftPressed, ")", "0")
            Case Keys.D1 : Return If(shiftPressed, "!", "1")
            Case Keys.D2 : Return If(shiftPressed, "@", "2")
            Case Keys.D3 : Return If(shiftPressed, "#", "3")
            Case Keys.D4 : Return If(shiftPressed, "$", "4")
            Case Keys.D5 : Return If(shiftPressed, "%", "5")
            Case Keys.D6 : Return If(shiftPressed, "^", "6")
            Case Keys.D7 : Return If(shiftPressed, "&", "7")
            Case Keys.D8 : Return If(shiftPressed, "*", "8")
            Case Keys.D9 : Return If(shiftPressed, "(", "9")
            Case Keys.OemComma : Return If(shiftPressed, "<", ",")
            Case Keys.OemPeriod : Return If(shiftPressed, ">", ".")
            Case Keys.OemQuestion : Return If(shiftPressed, "?", "/")
            Case Keys.OemSemicolon : Return If(shiftPressed, ":", ";")
            Case Keys.OemQuotes : Return If(shiftPressed, """", "'")
            Case Keys.OemTilde : Return If(shiftPressed, "~", "`")
            Case Else : Return Nothing
        End Select
    End Function

    Private Sub HandleMouseInput(mouseState As MouseState)
        Dim inputBoxRect As New Rectangle(
            ChatBoxMargin,
            _graphicsDevice.Viewport.Height - InputBoxHeight - ChatBoxMargin,
            _graphicsDevice.Viewport.Width - ChatBoxMargin * 2,
            InputBoxHeight
        )
        Dim sendButtonRect As New Rectangle(
            inputBoxRect.Right - SendButtonWidth - 5, inputBoxRect.Y + 5,
            SendButtonWidth, inputBoxRect.Height - 10
        )

        ' Handle send button click
        If mouseState.LeftButton = ButtonState.Pressed AndAlso
            _previousMouseState.LeftButton = ButtonState.Released AndAlso
            sendButtonRect.Contains(mouseState.Position) AndAlso
            _inputText.Length > 0 Then SendMessage()

        ' Handle scrollbar interaction
        Dim chatBoxRect As New Rectangle(
            ChatBoxMargin,
            ChatBoxMargin,
            _graphicsDevice.Viewport.Width - ChatBoxMargin * 2,
            _graphicsDevice.Viewport.Height - InputBoxHeight - ChatBoxMargin * 3
        )

        If mouseState.LeftButton = ButtonState.Pressed AndAlso
            _previousMouseState.LeftButton = ButtonState.Released AndAlso
            _scrollBarRect.Contains(mouseState.Position) AndAlso _maxScroll > 0 Then
            ' Start dragging scrollbar
            _isDraggingScrollBar = True
            _lastMouseDragPosition = New Vector2(mouseState.X, mouseState.Y)
        ElseIf mouseState.LeftButton = ButtonState.Released Then
            ' Stop dragging scrollbar
            _isDraggingScrollBar = False
        ElseIf _isDraggingScrollBar Then
            ' Continue dragging scrollbar
            Dim deltaY = mouseState.Y - _lastMouseDragPosition.Y
            _lastMouseDragPosition = New Vector2(mouseState.X, mouseState.Y)

            ' Calculate how much to scroll based on drag distance
            Dim scrollRatio = CSng(chatBoxRect.Height - 20 - _scrollBarRect.Height) / _maxScroll
            _scrollOffset = MathHelper.Clamp(_scrollOffset + CInt(deltaY / scrollRatio), 0, _maxScroll)
        End If

        ' Handle mouse wheel scrolling
        Dim mouseWheelDelta = mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue
        If mouseWheelDelta <> 0 AndAlso chatBoxRect.Contains(New Point(mouseState.X, mouseState.Y)) Then
            Dim scrollAmount = -Math.Sign(mouseWheelDelta) * 30 ' Adjust sensitivity
            _scrollOffset = MathHelper.Clamp(_scrollOffset + scrollAmount, 0, _maxScroll)
        End If
    End Sub

    Private Sub SendMessage()
        If String.IsNullOrWhiteSpace(_inputText) Then Exit Sub

        AddMessage("User", _inputText)
        _pendingBotReply = ChatbotReply(_inputText)
        _waitingForBotReply = True
        _inputActive = False
        _inputText = String.Empty
    End Sub

    Private Sub AddMessage(sender As String, text As String)
        _messages.Add(New ChatMessage With {
                    .Sender = sender,
                    .Text = text
                })
    End Sub

    Public Sub Draw(spriteBatch As SpriteBatch, screenWidth As Integer, screenHeight As Integer, Optional positionOffset As Point = DirectCast(Nothing, Point))
        Dim chatBoxRect = New Rectangle(ChatBoxMargin + positionOffset.X, ChatBoxMargin + positionOffset.Y, screenWidth - ChatBoxMargin * 2, screenHeight - InputBoxHeight - ChatBoxMargin * 3)

        Dim inputBoxRect = New Rectangle(ChatBoxMargin + positionOffset.X, positionOffset.Y + screenHeight - InputBoxHeight - ChatBoxMargin, screenWidth - ChatBoxMargin * 2, InputBoxHeight)

        Dim sendButtonRect = New Rectangle(inputBoxRect.Right - SendButtonWidth - 5, inputBoxRect.Y + 5, SendButtonWidth, inputBoxRect.Height - 10)

        Me.DrawChatBox(spriteBatch, chatBoxRect)
        Me.DrawInputBox(spriteBatch, inputBoxRect, sendButtonRect)
    End Sub

    Private Sub DrawChatBox(spriteBatch As SpriteBatch, rect As Rectangle)
        ' Draw chat box background
        spriteBatch.Draw(_pixel, rect, Color.WhiteSmoke)
        spriteBatch.Draw(_pixel, New Rectangle(rect.X, rect.Y, rect.Width, 2), Color.Gray)

        ' Draw messages first (to avoid overlapping scrollbar thumb)
        Dim yPos = rect.Y + 10 - _scrollOffset
        For Each message In _messages
            Dim textSize = _font.MeasureString(message.Text)
            Dim bubbleHeight = Math.Max(textSize.Y + BubblePadding * 2, IconSize + BubblePadding * 2)
            Dim bubbleWidth = textSize.X + BubblePadding * 2 + IconSize + MessagePadding

            ' Determine message position (left for AI, right for user)
            Dim isUserMessage = message.Sender Is "User"
            Dim bubbleX = If(isUserMessage, rect.Right - bubbleWidth - 20, rect.X + 10)

            ' Draw message bubble
            Dim bubbleRect = New Rectangle(CInt(MathF.Round(bubbleX)), yPos, CInt(bubbleWidth), CInt(bubbleHeight))
            Dim bubbleColor = If(isUserMessage, New Color(220, 248, 198), New Color(230, 230, 250)) ' Light green for user, light purple for AI
            spriteBatch.Draw(_pixel, bubbleRect, bubbleColor)

            ' Draw sender icon
            Dim iconTexture = If(isUserMessage, _userIcon, _aiIcon)
            If iconTexture IsNot Nothing Then
                Dim iconRect = New Rectangle(If(isUserMessage, bubbleRect.Right - IconSize - 5, bubbleRect.X + 5), CInt(bubbleRect.Y + (bubbleHeight - IconSize) / 2), IconSize, IconSize)
                spriteBatch.Draw(iconTexture, iconRect, Color.White)
            End If

            ' Draw message text
            Dim textPosition = New Vector2(If(isUserMessage, bubbleRect.Right - textSize.X - IconSize - MessagePadding, bubbleRect.X + IconSize + MessagePadding), bubbleRect.Y + (bubbleHeight - textSize.Y) / 2)
            spriteBatch.DrawString(_font, message.Text, textPosition, Color.Black)

            yPos += CInt(bubbleHeight) + MessagePadding
        Next

        ' Calculate total height needed for all messages
        Dim totalMessagesHeight = 0F
        For Each message In _messages
            Dim textSize = _font.MeasureString(message.Text)
            Dim bubbleHeight = Math.Max(textSize.Y + BubblePadding * 2, IconSize + BubblePadding * 2)
            totalMessagesHeight += bubbleHeight + MessagePadding
        Next

        ' Update scroll max based on content
        _maxScroll = Math.Max(0, CInt(totalMessagesHeight - rect.Height + 20)) ' +20 for top margin

        ' Calculate scrollbar dimensions
        Dim scrollBarHeight = Math.Max(ScrollBarMinHeight, CInt(rect.Height / Math.Max(totalMessagesHeight, rect.Height) * rect.Height))
        Dim scrollBarY = rect.Y + 10 + CInt(_scrollOffset / CSng(Math.Max(_maxScroll, 1)) * (rect.Height - 10 - scrollBarHeight))

        _scrollBarRect = New Rectangle(rect.Right - ScrollBarWidth - 5, scrollBarY, ScrollBarWidth, scrollBarHeight)

        ' Draw scrollbar background
        spriteBatch.Draw(_pixel, New Rectangle(_scrollBarRect.X - 2, rect.Y + 10, ScrollBarWidth + 4, rect.Height - 20), Color.LightGray)

        ' Draw scrollbar thumb
        spriteBatch.Draw(_pixel, _scrollBarRect, If(_isDraggingScrollBar, Color.DarkGray, Color.Gray))
    End Sub

    Private Sub DrawInputBox(spriteBatch As SpriteBatch, inputRect As Rectangle, sendButtonRect As Rectangle)
        spriteBatch.Draw(_pixel, inputRect, Color.White)
        spriteBatch.Draw(_pixel, New Rectangle(inputRect.X, inputRect.Y, inputRect.Width, 2), Color.Gray)

        Dim inputTextPos = New Vector2(inputRect.X + 10, inputRect.Y + 15)
        spriteBatch.DrawString(_font, _inputText & If(_inputActive, "|", ""), inputTextPos, Color.Black)

        spriteBatch.Draw(_pixel, sendButtonRect, Color.LightBlue)
        spriteBatch.Draw(_pixel, New Rectangle(sendButtonRect.X, sendButtonRect.Y, sendButtonRect.Width, 2), Color.Blue)

        Dim buttonText = "Send"
        Dim buttonTextSize = _font.MeasureString(buttonText)
        Dim buttonTextPos = New Vector2(sendButtonRect.X + (sendButtonRect.Width - buttonTextSize.X) / 2, sendButtonRect.Y + (sendButtonRect.Height - buttonTextSize.Y) / 2)
        spriteBatch.DrawString(_font, buttonText, buttonTextPos, Color.Black)
    End Sub

    Private Class ChatMessage
        Public Property Sender As String = String.Empty
        Public Property Text As String = String.Empty
    End Class

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Private Sub Dispose(disposing As Boolean)
        If Not _isDisposed Then
            If disposing Then
                _pixel.Dispose()
            End If
            _isDisposed = True
        End If
    End Sub
End Class