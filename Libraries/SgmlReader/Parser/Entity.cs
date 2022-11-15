using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text;
using Sgml.Parser.Enum;

namespace Sgml;

/// <summary>
/// An Entity declared in a DTD.
/// </summary>
public class Entity : IDisposable
{
    /// <summary>
    /// The character indicating End Of File.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "The capitalisation is correct since EOF is an acronym.")]
    public const char EOF = (char)65535;

    private string m_proxy;
    private string m_name;
    private bool m_isInternal;
    private string m_publicId;
    private string m_uri;
    private string m_literal;
    private LiteralType m_literalType;
    private Entity m_parent;
    private bool m_isHtml;
    private int m_line;
    private char m_lastchar;
    private bool m_isWhitespace;

    private Encoding m_encoding;
    private Uri m_resolvedUri;
    private TextReader m_stm;
    private bool m_weOwnTheStream;
    private int m_lineStart;
    private int m_absolutePos;

    /// <summary>
    /// Initialises a new instance of an Entity declared in a DTD.
    /// </summary>
    /// <param name="name">The name of the entity.</param>
    /// <param name="pubid">The public id of the entity.</param>
    /// <param name="uri">The uri of the entity.</param>
    /// <param name="proxy">The proxy server to use when retrieving any web content.</param>
    public Entity(string name, string pubid, string uri, string proxy)
    {
        m_name = name;
        m_publicId = pubid;
        m_uri = uri;
        m_proxy = proxy;
        m_isHtml = name != null && StringUtilities.EqualsIgnoreCase(name, "html");
    }

    /// <summary>
    /// Initialises a new instance of an Entity declared in a DTD.
    /// </summary>
    /// <param name="name">The name of the entity.</param>
    /// <param name="literal">The literal value of the entity.</param>
    public Entity(string name, string literal)
    {
        m_name = name;
        m_literal = literal;
        m_isInternal = true;
    }

    /// <summary>
    /// Initialises a new instance of an Entity declared in a DTD.
    /// </summary>
    /// <param name="name">The name of the entity.</param>
    /// <param name="baseUri">The baseUri for the entity to read from the TextReader.</param>
    /// <param name="stm">The TextReader to read the entity from.</param>
    /// <param name="proxy">The proxy server to use when retrieving any web content.</param>
    public Entity(string name, Uri baseUri, TextReader stm, string proxy)
    {
        m_name = name;
        m_isInternal = true;
        m_stm = stm;
        m_resolvedUri = baseUri;
        m_proxy = proxy;
        m_isHtml = string.Equals(name, "html", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The name of the entity.
    /// </summary>
    public string Name
    {
        get
        {
            return m_name;
        }
    }

    /// <summary>
    /// True if the entity is the html element entity.
    /// </summary>
    public bool IsHtml
    {
        get
        {
            return m_isHtml;
        }
        set
        {
            m_isHtml = value;
        }
    }

    /// <summary>
    /// The public identifier of this entity.
    /// </summary>
    public string PublicId
    {
        get
        {
            return m_publicId;
        }
    }

    /// <summary>
    /// The Uri that is the source for this entity.
    /// </summary>
    public string Uri
    {
        get
        {
            return m_uri;
        }
    }

    /// <summary>
    /// The resolved location of the DTD this entity is from.
    /// </summary>
    public Uri ResolvedUri
    {
        get
        {
            if (m_resolvedUri != null)
                return m_resolvedUri;
            else if (m_parent != null)
                return m_parent.ResolvedUri;
            else
                return null;
        }
    }

    /// <summary>
    /// Gets the parent Entity of this Entity.
    /// </summary>
    public Entity Parent
    {
        get
        {
            return m_parent;
        }
    }

    /// <summary>
    /// The last character read from the input stream for this entity.
    /// </summary>
    public char Lastchar
    {
        get
        {
            return m_lastchar;
        }
    }

    /// <summary>
    /// The line on which this entity was defined.
    /// </summary>
    public int Line
    {
        get
        {
            return m_line;
        }
    }

    /// <summary>
    /// The index into the line where this entity is defined.
    /// </summary>
    public int LinePosition
    {
        get
        {
            return m_absolutePos - m_lineStart + 1;
        }
    }

    /// <summary>
    /// Whether this entity is an internal entity or not.
    /// </summary>
    /// <value>true if this entity is internal, otherwise false.</value>
    public bool IsInternal
    {
        get
        {
            return m_isInternal;
        }
    }

    /// <summary>
    /// The literal value of this entity.
    /// </summary>
    public string Literal
    {
        get
        {
            return m_literal;
        }
    }

    /// <summary>
    /// The <see cref="LiteralType"/> of this entity.
    /// </summary>
    public LiteralType LiteralType
    {
        get
        {
            return m_literalType;
        }
    }

    /// <summary>
    /// Whether the last char read for this entity is a whitespace character.
    /// </summary>
    public bool IsWhitespace
    {
        get
        {
            return m_isWhitespace;
        }
    }

    /// <summary>
    /// The proxy server to use when making web requests to resolve entities.
    /// </summary>
    public string Proxy
    {
        get
        {
            return m_proxy;
        }
    }

    /// <summary>
    /// Reads the next character from the DTD stream.
    /// </summary>
    /// <returns>The next character from the DTD stream.</returns>
    public char ReadChar()
    {
        char ch = (char)m_stm.Read();
        if (ch == 0)
        {
            // convert nulls to whitespace, since they are not valid in XML anyway.
            ch = ' ';
        }
        m_absolutePos++;
        if (ch == 0xa)
        {
            m_isWhitespace = true;
            m_lineStart = m_absolutePos + 1;
            m_line++;
        }
        else if (ch == ' ' || ch == '\t')
        {
            m_isWhitespace = true;
            if (m_lastchar == 0xd)
            {
                m_lineStart = m_absolutePos;
                m_line++;
            }
        }
        else if (ch == 0xd)
        {
            m_isWhitespace = true;
        }
        else
        {
            m_isWhitespace = false;
            if (m_lastchar == 0xd)
            {
                m_line++;
                m_lineStart = m_absolutePos;
            }
        }

        m_lastchar = ch;
        return ch;
    }

    /// <summary>
    /// Begins processing an entity.
    /// </summary>
    /// <param name="parent">The parent of this entity.</param>
    /// <param name="baseUri">The base Uri for processing this entity within.</param>
    public void Open(Entity parent, Uri baseUri)
    {
        m_parent = parent;
        if (parent != null)
            m_isHtml = parent.IsHtml;
        m_line = 1;
        if (m_isInternal)
        {
            if (m_literal != null)
                m_stm = new StringReader(m_literal);
        }
        else if (m_uri == null)
        {
            Error("Unresolvable entity '{0}'", m_name);
        }
        else
        {
            if (baseUri != null)
            {
                m_resolvedUri = new Uri(baseUri, m_uri);
            }
            else
            {
                m_resolvedUri = new Uri(m_uri);
            }

            Stream stream = null;
            Encoding e = Encoding.Default;
            switch (m_resolvedUri.Scheme)
            {
                case "file":
                    {
                        string path = m_resolvedUri.LocalPath;
                        stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    }
                    break;
                default:
                    //Console.WriteLine("Fetching:" + ResolvedUri.AbsoluteUri);
                    HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(ResolvedUri);
                    wr.UserAgent = "Mozilla/4.0 (compatible;);";
                    wr.Timeout = 10000; // in case this is running in an ASPX page.
                    if (m_proxy != null)
                        wr.Proxy = new WebProxy(m_proxy);
                    wr.PreAuthenticate = false;
                    // Pass the credentials of the process. 
                    wr.Credentials = CredentialCache.DefaultCredentials;

                    WebResponse resp = wr.GetResponse();
                    Uri actual = resp.ResponseUri;
                    if (!string.Equals(actual.AbsoluteUri, m_resolvedUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                    {
                        m_resolvedUri = actual;
                    }
                    string contentType = resp.ContentType.ToLowerInvariant();
                    string mimeType = contentType;
                    int i = contentType.IndexOf(';');
                    if (i >= 0)
                    {
                        mimeType = contentType.Substring(0, i);
                    }

                    if (StringUtilities.EqualsIgnoreCase(mimeType, "text/html"))
                    {
                        m_isHtml = true;
                    }

                    i = contentType.IndexOf("charset");
                    e = Encoding.Default;
                    if (i >= 0)
                    {
                        int j = contentType.IndexOf("=", i);
                        int k = contentType.IndexOf(";", j);
                        if (k < 0)
                            k = contentType.Length;

                        if (j > 0)
                        {
                            j++;
                            string charset = contentType.Substring(j, k - j).Trim();
                            try
                            {
                                e = Encoding.GetEncoding(charset);
                            }
                            catch (ArgumentException)
                            {
                            }
                        }
                    }

                    stream = resp.GetResponseStream();
                    break;
            }

            m_weOwnTheStream = true;
            HtmlStream html = new HtmlStream(stream, e);
            m_encoding = html.Encoding;
            m_stm = html;
        }
    }

    /// <summary>
    /// Gets the character encoding for this entity.
    /// </summary>
    public Encoding Encoding
    {
        get
        {
            return m_encoding;
        }
    }

    /// <summary>
    /// Closes the reader from which the entity is being read.
    /// </summary>
    public void Close()
    {
        if (m_weOwnTheStream)
            m_stm.Close();
    }

    /// <summary>
    /// Returns the next character after any whitespace.
    /// </summary>
    /// <returns>The next character that is not whitespace.</returns>
    public char SkipWhitespace()
    {
        char ch = m_lastchar;
        while (ch != EOF && (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t'))
        {
            ch = ReadChar();
        }
        return ch;
    }

    /// <summary>
    /// Scans a token from the input stream and returns the result.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> to use to process the token.</param>
    /// <param name="term">A set of characters to look for as terminators for the token.</param>
    /// <param name="nmtoken">true if the token should be a NMToken, otherwise false.</param>
    /// <returns>The scanned token.</returns>
    public string ScanToken(StringBuilder sb, string term, bool nmtoken)
    {
        if (sb == null)
            throw new ArgumentNullException("sb");

        if (term == null)
            throw new ArgumentNullException("term");

        sb.Length = 0;
        char ch = m_lastchar;
        if (nmtoken && ch != '_' && !char.IsLetter(ch))
        {
            throw new SgmlParseException(string.Format(CultureInfo.CurrentUICulture, "Invalid name start character '{0}'", ch));
        }

        while (ch != EOF && term.IndexOf(ch) < 0)
        {
            if (!nmtoken || ch == '_' || ch == '.' || ch == '-' || ch == ':' || char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else
            {
                throw new SgmlParseException(
                    string.Format(CultureInfo.CurrentUICulture, "Invalid name character '{0}'", ch));
            }
            ch = ReadChar();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Read a literal from the input stream.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> to use to build the literal.</param>
    /// <param name="quote">The delimiter for the literal.</param>
    /// <returns>The literal scanned from the input stream.</returns>
    public string ScanLiteral(StringBuilder sb, char quote)
    {
        if (sb == null)
            throw new ArgumentNullException("sb");

        sb.Length = 0;
        char ch = ReadChar();
        while (ch != EOF && ch != quote)
        {
            if (ch == '&')
            {
                ch = ReadChar();
                if (ch == '#')
                {
                    string charent = ExpandCharEntity();
                    sb.Append(charent);
                    ch = m_lastchar;
                }
                else
                {
                    sb.Append('&');
                    sb.Append(ch);
                    ch = ReadChar();
                }
            }
            else
            {
                sb.Append(ch);
                ch = ReadChar();
            }
        }

        ReadChar(); // consume end quote.
        return sb.ToString();
    }

    /// <summary>
    /// Reads input until the end of the input stream or until a string of terminator characters is found.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> to use to build the string.</param>
    /// <param name="type">The type of the element being read (only used in reporting errors).</param>
    /// <param name="terminators">The string of terminator characters to look for.</param>
    /// <returns>The string read from the input stream.</returns>
    public string ScanToEnd(StringBuilder sb, string type, string terminators)
    {
        if (terminators == null)
            throw new ArgumentNullException("terminators");

        if (sb != null)
            sb.Length = 0;

        int start = m_line;
        // This method scans over a chunk of text looking for the
        // termination sequence specified by the 'terminators' parameter.
        char ch = ReadChar();
        int state = 0;
        char next = terminators[state];
        while (ch != EOF)
        {
            if (ch == next)
            {
                state++;
                if (state >= terminators.Length)
                {
                    // found it!
                    break;
                }
                next = terminators[state];
            }
            else if (state > 0)
            {
                // char didn't match, so go back and see how much does still match.
                int i = state - 1;
                int newstate = 0;
                while (i >= 0 && newstate == 0)
                {
                    if (terminators[i] == ch)
                    {
                        // character is part of the terminators pattern, ok, so see if we can
                        // match all the way back to the beginning of the pattern.
                        int j = 1;
                        while (i - j >= 0)
                        {
                            if (terminators[i - j] != terminators[state - j])
                                break;

                            j++;
                        }

                        if (j > i)
                        {
                            newstate = i + 1;
                        }
                    }
                    else
                    {
                        i--;
                    }
                }

                if (sb != null)
                {
                    i = i < 0 ? 1 : 0;
                    for (int k = 0; k <= state - newstate - i; k++)
                    {
                        sb.Append(terminators[k]);
                    }

                    if (i > 0) // see if we've matched this char or not
                        sb.Append(ch); // if not then append it to buffer.
                }

                state = newstate;
                next = terminators[newstate];
            }
            else
            {
                if (sb != null)
                    sb.Append(ch);
            }

            ch = ReadChar();
        }

        if (ch == 0)
            Error(type + " starting on line {0} was never closed", start);

        ReadChar(); // consume last char in termination sequence.
        if (sb != null)
            return sb.ToString();
        else
            return string.Empty;
    }

    /// <summary>
    /// Expands a character entity to be read from the input stream.
    /// </summary>
    /// <returns>The string for the character entity.</returns>
    public string ExpandCharEntity()
    {
        string value;
        int v = ReadNumericEntityCode(out value);
        if (v == -1)
        {
            return value;
        }

        // HACK ALERT: IE and Netscape map the unicode characters 
        if (m_isHtml && v >= 0x80 & v <= 0x9F)
        {
            // This range of control characters is mapped to Windows-1252!
            int i = v - 0x80;
            int unicode = CtrlMap[i];
            return Convert.ToChar(unicode).ToString();
        }

        if (0xD800 <= v && v <= 0xDBFF)
        {
            // high surrogate
            if (m_lastchar == '&')
            {
                char ch = ReadChar();
                if (ch == '#')
                {
                    string value2;
                    int v2 = ReadNumericEntityCode(out value2);
                    if (v2 == -1)
                    {
                        return value + ";" + value2;
                    }
                    if (0xDC00 <= v2 && v2 <= 0xDFFF)
                    {
                        // low surrogate
                        v = char.ConvertToUtf32((char)v, (char)v2);
                    }
                }
                else
                {
                    Error("Premature {0} parsing surrogate pair", ch);
                }
            }
            else
            {
                Error("Premature {0} parsing surrogate pair", m_lastchar);
            }
        }

        // NOTE (steveb): we need to use ConvertFromUtf32 to allow for extended numeric encodings
        return char.ConvertFromUtf32(v);
    }

    private int ReadNumericEntityCode(out string value)
    {
        int v = 0;
        char ch = ReadChar();
        value = "&#";
        if (ch == 'x')
        {
            bool sawHexDigit = false;
            value += "x";
            ch = ReadChar();
            for (; ch != EOF && ch != ';'; ch = ReadChar())
            {
                int p = 0;
                if (ch >= '0' && ch <= '9')
                {
                    p = ch - '0';
                    sawHexDigit = true;
                }
                else if (ch >= 'a' && ch <= 'f')
                {
                    p = ch - 'a' + 10;
                    sawHexDigit = true;
                }
                else if (ch >= 'A' && ch <= 'F')
                {
                    p = ch - 'A' + 10;
                    sawHexDigit = true;
                }
                else
                {
                    break; //we must be done!
                    //Error("Hex digit out of range '{0}'", (int)ch);
                }
                value += ch;
                v = v * 16 + p;
            }
            if (!sawHexDigit)
            {
                return -1;
            }
        }
        else
        {
            bool sawDigit = false;
            for (; ch != EOF && ch != ';'; ch = ReadChar())
            {
                if (ch >= '0' && ch <= '9')
                {
                    v = v * 10 + (ch - '0');
                    sawDigit = true;
                }
                else
                {
                    break; // we must be done!
                    //Error("Decimal digit out of range '{0}'", (int)ch);
                }
                value += ch;
            }
            if (!sawDigit)
            {
                return -1;
            }
        }
        if (ch == 0)
        {
            Error("Premature {0} parsing entity reference", ch);
        }
        else if (ch == ';')
        {
            ReadChar();
        }
        return v;
    }

    static int[] CtrlMap = new int[] {
        // This is the windows-1252 mapping of the code points 0x80 through 0x9f.
        8364, 129, 8218, 402, 8222, 8230, 8224, 8225, 710, 8240, 352, 8249, 338, 141,
        381, 143, 144, 8216, 8217, 8220, 8221, 8226, 8211, 8212, 732, 8482, 353, 8250,
        339, 157, 382, 376
    };

    /// <summary>
    /// Raise a processing error.
    /// </summary>
    /// <param name="msg">The error message to use in the exception.</param>
    /// <exception cref="SgmlParseException">Always thrown.</exception>
    public void Error(string msg)
    {
        throw new SgmlParseException(msg, this);
    }

    /// <summary>
    /// Raise a processing error.
    /// </summary>
    /// <param name="msg">The error message to use in the exception.</param>
    /// <param name="ch">The unexpected character causing the error.</param>
    /// <exception cref="SgmlParseException">Always thrown.</exception>
    public void Error(string msg, char ch)
    {
        string str = ch == EOF ? "EOF" : char.ToString(ch);
        throw new SgmlParseException(string.Format(CultureInfo.CurrentUICulture, msg, str), this);
    }

    /// <summary>
    /// Raise a processing error.
    /// </summary>
    /// <param name="msg">The error message to use in the exception.</param>
    /// <param name="x">The value causing the error.</param>
    /// <exception cref="SgmlParseException">Always thrown.</exception>
    public void Error(string msg, int x)
    {
        throw new SgmlParseException(string.Format(CultureInfo.CurrentUICulture, msg, x), this);
    }

    /// <summary>
    /// Raise a processing error.
    /// </summary>
    /// <param name="msg">The error message to use in the exception.</param>
    /// <param name="arg">The argument for the error.</param>
    /// <exception cref="SgmlParseException">Always thrown.</exception>
    public void Error(string msg, string arg)
    {
        throw new SgmlParseException(string.Format(CultureInfo.CurrentUICulture, msg, arg), this);
    }

    /// <summary>
    /// Returns a string giving information on how the entity is referenced and declared, walking up the parents until the top level parent entity is found.
    /// </summary>
    /// <returns>Contextual information for the entity.</returns>
    public string Context()
    {
        Entity p = this;
        StringBuilder sb = new StringBuilder();
        while (p != null)
        {
            string msg;
            if (p.m_isInternal)
            {
                msg = string.Format(CultureInfo.InvariantCulture, "\nReferenced on line {0}, position {1} of internal entity '{2}'", p.m_line, p.LinePosition, p.m_name);
            }
            else
            {
                msg = string.Format(CultureInfo.InvariantCulture, "\nReferenced on line {0}, position {1} of '{2}' entity at [{3}]", p.m_line, p.LinePosition, p.m_name, p.ResolvedUri.AbsolutePath);
            }
            sb.Append(msg);
            p = p.Parent;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Checks whether a token denotes a literal entity or not.
    /// </summary>
    /// <param name="token">The token to check.</param>
    /// <returns>true if the token is "CDATA", "SDATA" or "PI", otherwise false.</returns>
    public static bool IsLiteralType(string token)
    {
        return string.Equals(token, "CDATA", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(token, "SDATA", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(token, "PI", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sets the entity to be a literal of the type specified.
    /// </summary>
    /// <param name="token">One of "CDATA", "SDATA" or "PI".</param>
    public void SetLiteralType(string token)
    {
        switch (token)
        {
            case "CDATA":
                m_literalType = LiteralType.CDATA;
                break;
            case "SDATA":
                m_literalType = LiteralType.SDATA;
                break;
            case "PI":
                m_literalType = LiteralType.PI;
                break;
        }
    }

    #region IDisposable Members

    /// <summary>
    /// The finalizer for the Entity class.
    /// </summary>
    ~Entity()
    {
        Dispose(false);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. 
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. 
    /// </summary>
    /// <param name="isDisposing">true if this method has been called by user code, false if it has been called through a finalizer.</param>
    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            if (m_stm != null)
            {
                m_stm.Dispose();
                m_stm = null;
            }
        }
    }

    #endregion
}