namespace CommandRouter.Tests;

using System;
using System.Linq;
using Xunit;

public class DefaultCommandTokenizerTests
{
    [Theory]
    [InlineData("say \"hello", 4)]
    [InlineData("say \"hello world", 4)]
    [InlineData("say \"", 4)]
    [InlineData("say \"hello\" \"world", 12)]
    public void TryTokenize_UnterminatedQuote_ReturnsFalseWithPosition(string input, int expectedPosition)
    {
        var tokenizer = new DefaultCommandTokenizer();

        var result = tokenizer.TryTokenize(input, out var command, out var errorPosition);

        Assert.False(result);
        Assert.Null(command);
        Assert.Equal(expectedPosition, errorPosition);
    }

    [Fact]
    public void TryTokenize_TerminatedQuote_ReturnsQuotedToken()
    {
        var tokenizer = new DefaultCommandTokenizer();

        var result = tokenizer.TryTokenize("say \"hello world\"", out var command, out var errorPosition);

        Assert.True(result);
        Assert.NotNull(command);
        Assert.Equal(["say", "hello world"], command.Tokens.Select(t => t.Value));
        Assert.Equal(-1, errorPosition);
    }

    [Theory]
    [InlineData("say \"a   b\"", "a   b")]
    [InlineData("say \"  padded  \"", "  padded  ")]
    [InlineData("say \"tab\tbed\"", "tab\tbed")]
    [InlineData("say \"\"", "")]
    public void TryTokenize_QuotedValue_IsKeptVerbatim(string input, string expectedValue)
    {
        var tokenizer = new DefaultCommandTokenizer();

        var result = tokenizer.TryTokenize(input, out var command, out _);

        Assert.True(result);
        Assert.NotNull(command);
        Assert.Equal(["say", expectedValue], command.Tokens.Select(t => t.Value));
        Assert.Equal(input.Length - 4, command.Tokens[1].Length);
    }

    [Fact]
    public void TryTokenize_EmptyString_ReturnsNoTokens()
    {
        var tokenizer = new DefaultCommandTokenizer();

        var result = tokenizer.TryTokenize("", out var command, out _);

        Assert.True(result);
        Assert.NotNull(command);
        Assert.Empty(command.Tokens);
    }

    [Fact]
    public void TryTokenize_KeepsRawString()
    {
        var tokenizer = new DefaultCommandTokenizer();

        tokenizer.TryTokenize("  say   hi ", out var command, out _);

        Assert.NotNull(command);
        Assert.Equal("  say   hi ", command.Raw);
    }

    [Fact]
    public void TryTokenize_ReturnsTokenPositionsInRawString()
    {
        var tokenizer = new DefaultCommandTokenizer();

        var result = tokenizer.TryTokenize("  say  \"hello world\" bob", out var command, out _);

        Assert.True(result);
        Assert.NotNull(command);
        const string raw = "  say  \"hello world\" bob";
        Assert.Equal(
            [
                new CommandToken(raw, 2, 3, false),
                new CommandToken(raw, 7, 13, true),
                new CommandToken(raw, 21, 3, false)
            ],
            command.Tokens);
        Assert.Equal("\"hello world\"", command.Raw.Substring(7, 13));
    }

    [Fact]
    public void TryTokenize_TokensExposeTheirValueAsASliceOfTheRawString()
    {
        var tokenizer = new DefaultCommandTokenizer();

        tokenizer.TryTokenize("say \"hello world\" bob", out var command, out _);

        Assert.NotNull(command);
        Assert.True(command.Tokens[0].ValueSpan.SequenceEqual("say"));
        Assert.True(command.Tokens[1].ValueSpan.SequenceEqual("hello world"));
        Assert.True(command.Tokens[2].ValueSpan.SequenceEqual("bob"));
        Assert.False(command.Tokens[0].IsQuoted);
        Assert.True(command.Tokens[1].IsQuoted);
    }

    [Fact]
    public void CommandToken_Default_HasEmptyValue()
    {
        var token = default(CommandToken);

        Assert.Equal("", token.Value);
        Assert.True(token.ValueSpan.IsEmpty);
    }

    [Fact]
    public void CommandToken_Equality_ComparesRawTextAndPosition()
    {
        var first = new CommandToken("say hi", 0, 3, false);
        var same = new CommandToken(new string("say hi".AsSpan()), 0, 3, false);

        Assert.Equal(first, same);
        Assert.NotEqual(first, new CommandToken("say hi", 4, 2, false));
        Assert.NotEqual(first, new CommandToken("sax hi", 0, 3, false));
    }
}