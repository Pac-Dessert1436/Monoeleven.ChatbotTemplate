Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports System.Text.RegularExpressions
Imports System.Threading

Public NotInheritable Class ChatUI
    Implements IDisposable

    Private ReadOnly _font As SpriteFont
    Private ReadOnly _pixel As Texture2D
    Private ReadOnly _graphicsDevice As GraphicsDevice
    Private _userIcon As Texture2D
    Private _aiIcon As Texture2D
    Private _isDisposed As Boolean = False

    Private ReadOnly _messages As New List(Of ChatMessage)
    Private ReadOnly _conversationHistory As New List(Of Message)
    Private _inputText As String = String.Empty
    Private _inputActive As Boolean = True
    Private _caretIndex As Integer = 0
    Private _inputScrollX As Single = 0F
    Private _waitingForBotReply As Boolean = False
    Private _pendingBotReply As String = String.Empty
    Private _previousKeyboardState As KeyboardState
    Private _previousMouseState As MouseState
    Private _currentReplyTask As Task = Nothing
    Private _cancellationTokenSource As CancellationTokenSource = Nothing

    ' Scrolling variables
    Private _scrollOffset As Integer = 0
    Private _maxScroll As Integer = 0
    Private _scrollBarRect As Rectangle
    Private _isDraggingScrollBar As Boolean = False
    Private _lastMouseDragPosition As Vector2
    Private _chatboxRenderTarget As RenderTarget2D

    Private Const CHAT_BOX_MARGIN As Integer = 10
    Private Const INPUT_BOX_HEIGHT As Integer = 50
    Private Const SEND_BUTTON_WIDTH As Integer = 80
    Private Const LINE_SPACING As Integer = 5
    Private Const MAX_LINE_WIDTH As Integer = 500
    Private Const MESSAGE_PADDING As Integer = 10
    Private Const BUBBLE_PADDING As Integer = 15
    Private Const ICON_SIZE As Integer = 32
    Private Const SCROLL_BAR_WIDTH As Integer = 10
    Private Const SCROLL_BAR_MIN_HEIGHT As Integer = 20
    Private Const H_SCROLL_BAR_HEIGHT As Integer = 6

    Public Sub New(font As SpriteFont, graphicsDevice As GraphicsDevice)
        _font = font
        _graphicsDevice = graphicsDevice
        _pixel = New Texture2D(graphicsDevice, 1, 1)
        _pixel.SetData({Color.White})

        _previousKeyboardState = Keyboard.GetState()
        _previousMouseState = Mouse.GetState()

        AddMessage("MGLLM", "Hello! I am MonoGame LLM. How can I help you?")
    End Sub

    ' Add method to clear conversation history
    Public Sub ClearConversation()
        _messages.Clear()
        _conversationHistory.Clear()
        _scrollOffset = 0
        _maxScroll = 0
        AddMessage("MGLLM", "Hello! I am MonoGame LLM. How can I help you?")
    End Sub

    Public Sub LoadIcons(userIcon As Texture2D, aiIcon As Texture2D)
        _userIcon = userIcon
        _aiIcon = aiIcon
    End Sub

    Public Sub Update(screenHeight As Integer)
        Dim keyboardState = Keyboard.GetState()
        Dim mouseState = Mouse.GetState()

        If _inputActive AndAlso Not _waitingForBotReply Then
            HandleTextInput(keyboardState)
            HandleMouseInput(mouseState, screenHeight)
        End If

        If _waitingForBotReply AndAlso Not String.IsNullOrEmpty(_pendingBotReply) Then
            AddMessage("MGLLM", _pendingBotReply)
            _pendingBotReply = String.Empty
        End If

        _previousKeyboardState = keyboardState
        _previousMouseState = mouseState
    End Sub

    Private Sub HandleTextInput(keyboardState As KeyboardState)
        For Each key In keyboardState.GetPressedKeys()
            If Not _previousKeyboardState.IsKeyDown(key) Then
                ' Submit on Enter
                If key = Keys.Enter AndAlso _inputText.Length > 0 Then
                    SendMessage()
                    Exit Sub
                End If

                ' Backspace: remove character before caret
                If key = Keys.Back Then
                    If _caretIndex > 0 AndAlso _inputText.Length > 0 Then
                        _inputText = _inputText.Remove(_caretIndex - 1, 1)
                        _caretIndex = Math.Max(0, _caretIndex - 1)
                    End If
                    Continue For
                End If

                ' Delete: remove character at caret
                If key = Keys.Delete Then
                    If _caretIndex < _inputText.Length Then
                        _inputText = _inputText.Remove(_caretIndex, 1)
                    End If
                    Continue For
                End If

                ' Move caret left/right/home/end
                If key = Keys.Left Then
                    _caretIndex = Math.Max(0, _caretIndex - 1)
                    Continue For
                ElseIf key = Keys.Right Then
                    _caretIndex = Math.Min(_inputText.Length, _caretIndex + 1)
                    Continue For
                ElseIf key = Keys.Home Then
                    _caretIndex = 0
                    Continue For
                ElseIf key = Keys.End Then
                    _caretIndex = _inputText.Length
                    Continue For
                End If

                ' Handle Ctrl+L to clear conversation
                If key = Keys.L AndAlso (keyboardState.IsKeyDown(Keys.LeftControl) _
                   OrElse keyboardState.IsKeyDown(Keys.RightControl)) Then
                    ClearConversation()
                    Continue For
                End If

                Dim character = GetCharacterFromKey(key, keyboardState)
                If Not Equals(character, Nothing) Then
                    _inputText = _inputText.Insert(_caretIndex, character)
                    _caretIndex += 1
                End If
            End If
        Next
    End Sub

    Private Shared Function GetCharacterFromKey(key As Keys, keyboardState As KeyboardState) As String
        Dim shiftPressed = keyboardState.IsKeyDown(Keys.LeftShift) OrElse
            keyboardState.IsKeyDown(Keys.RightShift)

        Select Case key
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
            Case Keys.OemMinus : Return If(shiftPressed, "_", "-")
            Case Keys.OemPlus : Return If(shiftPressed, "+", "=")
            Case Keys.OemPipe : Return If(shiftPressed, "|", "\")
            Case Else : Return Nothing
        End Select
    End Function

    Private Function WrapText(text As String, maxWidth As Single) As List(Of String)
        ' First clean up any markdown artifacts
        Dim cleanedText = text
        ' Remove markdown bold/italic markers
        cleanedText = Regex.Replace(cleanedText, "\*\*|\*|__|_", "")
        ' Remove extra whitespace
        cleanedText = Regex.Replace(cleanedText, "\s+", " ").Trim()

        Dim words = cleanedText.Split(" "c)
        Dim lines As New List(Of String)()
        Dim currentLine As New Text.StringBuilder
        Dim currentLineWidth As Single = 0F

        For Each word In words
            If String.IsNullOrWhiteSpace(word) Then Continue For

            Dim wordWidth = _font.MeasureString(word).X
            Dim spaceWidth = If(currentLine.Length > 0, _font.MeasureString(" ").X, 0F)

            If currentLine.Length = 0 Then
                currentLine.Append(word)
                currentLineWidth = wordWidth
            ElseIf currentLineWidth + spaceWidth + wordWidth <= maxWidth Then
                currentLine.Append(" " & word)
                currentLineWidth += spaceWidth + wordWidth
            Else
                lines.Add(currentLine.ToString())
                currentLine.Clear()
                currentLine.Append(word)
                currentLineWidth = wordWidth
            End If
        Next word
        If currentLine.Length > 0 Then lines.Add(currentLine.ToString())
        If lines.Count = 0 Then lines.Add(cleanedText)
        Return lines
    End Function

    Private Sub HandleMouseInput(mouseState As MouseState, screenHeight As Integer)
        Dim inputBoxRect As New Rectangle(
            CHAT_BOX_MARGIN,
            screenHeight - INPUT_BOX_HEIGHT - CHAT_BOX_MARGIN,
            _graphicsDevice.Viewport.Width - (CHAT_BOX_MARGIN * 2),
            INPUT_BOX_HEIGHT
        )
        Dim sendButtonRect As New Rectangle(
            inputBoxRect.Right - SEND_BUTTON_WIDTH - 5, inputBoxRect.Y + 5,
            SEND_BUTTON_WIDTH, inputBoxRect.Height - 10
        )

        ' Handle send button click
        If mouseState.LeftButton = ButtonState.Pressed AndAlso
            _previousMouseState.LeftButton = ButtonState.Released AndAlso
            sendButtonRect.Contains(mouseState.Position) AndAlso
            _inputText.Length > 0 Then SendMessage()

        ' Handle clicking inside input area to set caret
        Dim innerInputRect As New Rectangle(inputBoxRect.X + 5, inputBoxRect.Y + 5, sendButtonRect.X - inputBoxRect.X - 15, inputBoxRect.Height - 10)
        If mouseState.LeftButton = ButtonState.Pressed AndAlso _previousMouseState.LeftButton = ButtonState.Released AndAlso innerInputRect.Contains(mouseState.Position) Then
            ' Determine approximate caret index by measuring text width per character
            Dim clickX = mouseState.X - (innerInputRect.X + 10) + CInt(_inputScrollX)
            Dim accum As Single = 0F
            Dim foundIndex = 0
            For i = 0 To _inputText.Length - 1
                Dim chWidth = _font.MeasureString(_inputText.Substring(i, 1)).X
                If accum + (chWidth / 2) >= clickX Then
                    foundIndex = i
                    Exit For
                End If
                accum += chWidth
                foundIndex = i + 1
            Next
            _caretIndex = Math.Max(0, Math.Min(_inputText.Length, foundIndex))
        End If

        ' Handle scrollbar interaction
        Dim chatBoxRect As New Rectangle(
            CHAT_BOX_MARGIN,
            CHAT_BOX_MARGIN,
            _graphicsDevice.Viewport.Width - (CHAT_BOX_MARGIN * 2),
            screenHeight - INPUT_BOX_HEIGHT - (CHAT_BOX_MARGIN * 3)
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
        If _waitingForBotReply Then Exit Sub

        AddMessage("User", _inputText)
        _waitingForBotReply = True
        _inputActive = False
        _inputText = String.Empty
        _caretIndex = 0
        _inputScrollX = 0F

        ' Start async task to get bot reply
        _cancellationTokenSource?.Cancel()
        _cancellationTokenSource = New CancellationTokenSource()
        Dim cancellationToken = _cancellationTokenSource.Token
        Dim userMessage = _messages.Last().Text

        _currentReplyTask = Task.Run(
            Async Function()
                Try
                    Dim reply = Await GetDeepSeekResponseAsync(userMessage, _conversationHistory)
                    If Not cancellationToken.IsCancellationRequested Then
                        _pendingBotReply = reply
                        ' Add to conversation history
                        _conversationHistory.Add(New Message With {.Role = "user", .Content = userMessage})
                        _conversationHistory.Add(New Message With {.Role = "assistant", .Content = reply})
                        ' Save chat log
                        SaveChatLog(userMessage, reply, NextConversationIndex)
                    End If
                Catch ex As Exception
                    If Not cancellationToken.IsCancellationRequested Then
                        _pendingBotReply = "Error: " & ex.Message
                    End If
                Finally
                    If Not cancellationToken.IsCancellationRequested Then
                        _waitingForBotReply = False
                        _inputActive = True
                    End If
                End Try
            End Function, cancellationToken)
    End Sub

    Private Sub AddMessage(sender As String, text As String)
        _messages.Add(New ChatMessage With {.Sender = sender, .Text = text})
        ' Calculate max scroll immediately after adding message
        CalculateMaxScroll()
        ' Auto-scroll to bottom when new message is added
        _scrollOffset = _maxScroll
    End Sub

    Private Sub CalculateMaxScroll()
        Dim totalMessagesHeight = 0F
        For Each message As ChatMessage In _messages
            Dim wrappedLines = WrapText(message.Text, MAX_LINE_WIDTH)
            Dim maxLineWidth As Single = 0F
            For Each line As String In wrappedLines
                Dim lineWidth = _font.MeasureString(line).X
                If lineWidth > maxLineWidth Then
                    maxLineWidth = lineWidth
                End If
            Next line

            Dim totalTextHeight = (wrappedLines.Count * _font.LineSpacing) + ((wrappedLines.Count - 1) * LINE_SPACING)
            Dim bubbleHeight = Math.Max(totalTextHeight + (BUBBLE_PADDING * 2), ICON_SIZE + (BUBBLE_PADDING * 2))
            totalMessagesHeight += bubbleHeight + MESSAGE_PADDING
        Next message

        Dim chatBoxHeight = _graphicsDevice.Viewport.Height - INPUT_BOX_HEIGHT - (CHAT_BOX_MARGIN * 3)
        _maxScroll = Math.Max(0, CInt(totalMessagesHeight - chatBoxHeight + 20))
    End Sub

    Public Sub Draw(spriteBatch As SpriteBatch, screenWidth As Integer, screenHeight As Integer, Optional positionOffset As Point = Nothing)
        Dim chatBoxRect As New Rectangle(
            CHAT_BOX_MARGIN + positionOffset.X,
            CHAT_BOX_MARGIN + positionOffset.Y,
            screenWidth - (CHAT_BOX_MARGIN * 2),
            screenHeight - INPUT_BOX_HEIGHT - (CHAT_BOX_MARGIN * 3))
        Dim inputBoxRect As New Rectangle(
            CHAT_BOX_MARGIN + positionOffset.X,
            positionOffset.Y + screenHeight - INPUT_BOX_HEIGHT - CHAT_BOX_MARGIN,
            screenWidth - (CHAT_BOX_MARGIN * 2), INPUT_BOX_HEIGHT)
        Dim sendButtonRect As New Rectangle(
            inputBoxRect.Right - SEND_BUTTON_WIDTH - 5,
            inputBoxRect.Y + 5, SEND_BUTTON_WIDTH,
            inputBoxRect.Height - 10)

        DrawChatBox(spriteBatch, chatBoxRect)
        DrawInputBox(spriteBatch, inputBoxRect, sendButtonRect)
    End Sub

    ' TODO: Introduce RenderTarget2D for chat box area
    Private Sub DrawChatBox(spriteBatch As SpriteBatch, rect As Rectangle)
        ' Initialize or resize render target if needed
        If _chatboxRenderTarget Is Nothing OrElse _chatboxRenderTarget.Width <> rect.Width OrElse
            _chatboxRenderTarget.Height <> rect.Height Then
            _chatboxRenderTarget?.Dispose()
            _chatboxRenderTarget = New RenderTarget2D(_graphicsDevice, rect.Width, rect.Height)
        End If

        ' Set render target
        _graphicsDevice.SetRenderTarget(_chatboxRenderTarget)
        _graphicsDevice.Clear(Color.WhiteSmoke)

        ' Draw content to render target
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend)
        ' Draw top border
        spriteBatch.Draw(_pixel, New Rectangle(0, 0, rect.Width, 2), Color.Gray)

        ' Draw messages
        Dim yPos = 10 - _scrollOffset
        For Each message In _messages
            Dim wrappedLines = WrapText(message.Text, MAX_LINE_WIDTH)
            Dim maxLineWidth As Single = 0F
            For Each line In wrappedLines
                Dim lineWidth = _font.MeasureString(line).X
                If lineWidth > maxLineWidth Then maxLineWidth = lineWidth
            Next line

            Dim totalTextHeight = (wrappedLines.Count * _font.LineSpacing) + ((wrappedLines.Count - 1) * LINE_SPACING)
            Dim bubbleHeight = Math.Max(totalTextHeight + (BUBBLE_PADDING * 2), ICON_SIZE + (BUBBLE_PADDING * 2))
            Dim bubbleWidth = maxLineWidth + (BUBBLE_PADDING * 2) + ICON_SIZE + MESSAGE_PADDING

            Dim isUserMessage = message.Sender Is "User"
            Dim bubbleX = If(isUserMessage, rect.Width - bubbleWidth - 20, 10)

            Dim bubbleRect = New Rectangle(
                CInt(MathF.Round(bubbleX)), yPos, CInt(bubbleWidth), bubbleHeight
            )
            Dim bubbleColor = If(isUserMessage, New Color(220, 248, 198), New Color(230, 230, 250))
            spriteBatch.Draw(_pixel, bubbleRect, bubbleColor)

            Dim iconTexture = If(isUserMessage, _userIcon, _aiIcon)
            If iconTexture IsNot Nothing Then
                Dim iconRect = New Rectangle(
                    If(isUserMessage, bubbleRect.Right - ICON_SIZE - 5, bubbleRect.X + 5),
                        CInt(bubbleRect.Y + ((bubbleHeight - ICON_SIZE) / 2)), ICON_SIZE, ICON_SIZE)
                spriteBatch.Draw(iconTexture, iconRect, Color.White)
            End If

            Dim textStartY = bubbleRect.Y + ((bubbleHeight - totalTextHeight) / 2)
            For i = 0 To wrappedLines.Count - 1
                Dim lineText = wrappedLines(i)
                Dim textSize = _font.MeasureString(lineText)
                Dim textPosition = New Vector2(
                    If(isUserMessage, bubbleRect.Right - textSize.X - ICON_SIZE - MESSAGE_PADDING,
                        bubbleRect.X + ICON_SIZE + MESSAGE_PADDING),
                    CSng(textStartY + (i * (_font.LineSpacing + LINE_SPACING))))
                spriteBatch.DrawString(_font, lineText, textPosition, Color.Black)
            Next

            yPos += bubbleHeight + MESSAGE_PADDING
        Next

        ' Calculate scroll values
        CalculateMaxScroll()
        ' Adjust maxScroll based on current chat box height
        _maxScroll = Math.Max(0, _maxScroll - (rect.Height - (_graphicsDevice.Viewport.Height - INPUT_BOX_HEIGHT - (CHAT_BOX_MARGIN * 3))))

        Dim totalMessagesHeight = _maxScroll + rect.Height - 20
        Dim scrollBarHeight = Math.Max(SCROLL_BAR_MIN_HEIGHT, CInt(rect.Height / Math.Max(totalMessagesHeight, rect.Height) * rect.Height))
        Dim scrollBarY = 10 + CInt(_scrollOffset / CSng(Math.Max(_maxScroll, 1)) * (rect.Height - 10 - scrollBarHeight))

        _scrollBarRect = New Rectangle(rect.Width - SCROLL_BAR_WIDTH - 5, scrollBarY, SCROLL_BAR_WIDTH, scrollBarHeight)

        ' Draw scrollbar track
        spriteBatch.Draw(_pixel, New Rectangle(_scrollBarRect.X - 2, 10, SCROLL_BAR_WIDTH + 4, rect.Height - 20), Color.LightGray)

        ' Draw scrollbar thumb
        spriteBatch.Draw(_pixel, _scrollBarRect, If(_isDraggingScrollBar, Color.DarkGray, Color.Gray))
        spriteBatch.End()

        ' Reset render target and draw the chatbox to the screen
        _graphicsDevice.SetRenderTarget(Nothing)
        _graphicsDevice.Clear(Color.AntiqueWhite)
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend)
        spriteBatch.Draw(_chatboxRenderTarget, New Vector2(rect.X, rect.Y), Color.White)
        ' NOTE: Do not call spriteBatch.End() here; the SpriteBatch continues drawing graphics
        '       on the GameMain.Draw() call.
    End Sub

    Private Sub DrawInputBox(spriteBatch As SpriteBatch, inputRect As Rectangle, sendButtonRect As Rectangle)
        ' Draw full input background area
        spriteBatch.Draw(_pixel, inputRect, Color.White)
        spriteBatch.Draw(_pixel, New Rectangle(inputRect.X, inputRect.Y, inputRect.Width, 2), Color.Gray)

        ' Inner input area (stop before the send button)
        Dim innerRect As New Rectangle(inputRect.X + 5, inputRect.Y + 5, sendButtonRect.X - inputRect.X - 15, inputRect.Height - 10)
        spriteBatch.Draw(_pixel, innerRect, Color.White)

        ' Padding for text inside input
        Dim leftPadding = 10, rightPadding = 10

        ' Measure cumulative widths to support caret and scrolling
        Dim cumWidths(_inputText.Length) As Single
        cumWidths(0) = 0F
        For i = 1 To _inputText.Length
            cumWidths(i) = cumWidths(i - 1) + _font.MeasureString(_inputText.Substring(i - 1, 1)).X
        Next

        Dim textTotalWidth = If(_inputText.Length = 0, 0F, cumWidths(_inputText.Length))

        ' Ensure caret visibility by adjusting _inputScrollX
        Dim caretPixelPos = If(_caretIndex >= 0 AndAlso _caretIndex <= _inputText.Length, cumWidths(_caretIndex), 0F)
        Dim visibleWidth = innerRect.Width - leftPadding - rightPadding
        If caretPixelPos - _inputScrollX > visibleWidth Then
            _inputScrollX = caretPixelPos - visibleWidth
        ElseIf caretPixelPos - _inputScrollX < 0 Then
            _inputScrollX = caretPixelPos
        End If
        _inputScrollX = Math.Max(0F, Math.Min(_inputScrollX, Math.Max(0F, textTotalWidth - visibleWidth)))

        ' Find visible substring range
        Dim startIndex = 0
        For i = 0 To _inputText.Length
            If cumWidths(i) >= _inputScrollX Then
                startIndex = i
                Exit For
            End If
        Next i
        Dim endIndex = startIndex
        For i = startIndex To _inputText.Length
            If cumWidths(i) - _inputScrollX > visibleWidth Then
                endIndex = i
                Exit For
            End If
            endIndex = i
        Next i
        endIndex = Math.Min(_inputText.Length, Math.Max(startIndex, endIndex))

        Dim visibleText = If(endIndex > startIndex, _inputText.Substring(startIndex, endIndex - startIndex), String.Empty)
        Dim drawX = innerRect.X + leftPadding + (cumWidths(startIndex) - _inputScrollX)
        Dim drawY = innerRect.Y + ((innerRect.Height - _font.LineSpacing) \ 2)
        spriteBatch.DrawString(_font, visibleText, New Vector2(drawX, drawY), Color.Black)

        ' Draw caret as a thin vertical line
        If _inputActive Then
            Dim caretX = innerRect.X + leftPadding + (caretPixelPos - _inputScrollX)
            Dim caretY = innerRect.Y + ((innerRect.Height - _font.LineSpacing) / 2)
            Dim caretRect = New Rectangle(CInt(Math.Round(caretX)), CInt(caretY), 2, _font.LineSpacing)
            spriteBatch.Draw(_pixel, caretRect, Color.Black)
        End If

        ' Draw optional horizontal scrollbar when text is wider than visible area
        If textTotalWidth > visibleWidth Then
            Dim hBarWidth = CInt(visibleWidth / textTotalWidth * visibleWidth)
            hBarWidth = Math.Max(20, hBarWidth)
            Dim hBarX = innerRect.X + CInt((_inputScrollX / Math.Max(1.0F, textTotalWidth - visibleWidth)) * (visibleWidth - hBarWidth))
            Dim hBarY = innerRect.Bottom - H_SCROLL_BAR_HEIGHT - 4
            spriteBatch.Draw(_pixel, New Rectangle(innerRect.X, hBarY, innerRect.Width, H_SCROLL_BAR_HEIGHT), Color.LightGray)
            spriteBatch.Draw(_pixel, New Rectangle(hBarX, hBarY, hBarWidth, H_SCROLL_BAR_HEIGHT), Color.Gray)
        End If

        ' Draw send button
        spriteBatch.Draw(_pixel, sendButtonRect, Color.LightBlue)
        spriteBatch.Draw(_pixel, New Rectangle(sendButtonRect.X, sendButtonRect.Y, sendButtonRect.Width, 2), Color.Blue)

        Dim buttonText = "Send"
        Dim buttonTextSize = _font.MeasureString(buttonText)
        Dim buttonTextPos = New Vector2(sendButtonRect.X + ((sendButtonRect.Width - buttonTextSize.X) / 2), sendButtonRect.Y + ((sendButtonRect.Height - buttonTextSize.Y) / 2))
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
                _chatboxRenderTarget?.Dispose()
                _cancellationTokenSource?.Cancel()
                _cancellationTokenSource?.Dispose()
                If _currentReplyTask IsNot Nothing AndAlso Not _currentReplyTask.IsCompleted Then
                    Try
                        _currentReplyTask.Wait(1000)
                    Catch
                        ' Ignore cancellation exceptions
                    End Try
                End If
            End If
            _isDisposed = True
        End If
    End Sub
End Class