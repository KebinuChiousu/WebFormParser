using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using Sgml.Parser.Enum;

namespace Sgml;

/// <summary>
/// Provides DTD parsing and support for the SgmlParser framework.
/// </summary>
public class SgmlDtd
{
    private string m_name;

    private Dictionary<string, ElementDecl> m_elements;
    private Dictionary<string, Entity> m_pentities;
    private Dictionary<string, Entity> m_entities;
    private StringBuilder m_sb;
    private Entity m_current;

    /// <summary>
    /// Initialises a new instance of the <see cref="SgmlDtd"/> class.
    /// </summary>
    /// <param name="name">The name of the DTD.</param>
    /// <param name="nt">The <see cref="XmlNameTable"/> is NOT used.</param>
    public SgmlDtd(string name, XmlNameTable nt)
    {
        m_name = name;
        m_elements = new Dictionary<string, ElementDecl>();
        m_pentities = new Dictionary<string, Entity>();
        m_entities = new Dictionary<string, Entity>();
        m_sb = new StringBuilder();
    }

    /// <summary>
    /// The name of the DTD.
    /// </summary>
    public string Name
    {
        get
        {
            return m_name;
        }
    }

    /// <summary>
    /// Gets the XmlNameTable associated with this implementation.
    /// </summary>
    /// <value>The XmlNameTable enabling you to get the atomized version of a string within the node.</value>
    public XmlNameTable NameTable
    {
        get
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a DTD and creates a <see cref="SgmlDtd"/> instance that encapsulates the DTD.
    /// </summary>
    /// <param name="baseUri">The base URI of the DTD.</param>
    /// <param name="name">The name of the DTD.</param>
    /// <param name="pubid"></param>
    /// <param name="url"></param>
    /// <param name="subset"></param>
    /// <param name="proxy"></param>
    /// <param name="nt">The <see cref="XmlNameTable"/> is NOT used.</param>
    /// <returns>A new <see cref="SgmlDtd"/> instance that encapsulates the DTD.</returns>
    public static SgmlDtd Parse(Uri baseUri, string name, string pubid, string url, string subset, string proxy, XmlNameTable nt)
    {
        SgmlDtd dtd = new SgmlDtd(name, nt);
        if (!string.IsNullOrEmpty(url))
        {
            dtd.PushEntity(baseUri, new Entity(dtd.Name, pubid, url, proxy));
        }

        if (!string.IsNullOrEmpty(subset))
        {
            dtd.PushEntity(baseUri, new Entity(name, subset));
        }

        try
        {
            dtd.Parse();
        }
        catch (ApplicationException e)
        {
            throw new SgmlParseException(e.Message + dtd.m_current.Context());
        }

        return dtd;
    }

    /// <summary>
    /// Parses a DTD and creates a <see cref="SgmlDtd"/> instance that encapsulates the DTD.
    /// </summary>
    /// <param name="baseUri">The base URI of the DTD.</param>
    /// <param name="name">The name of the DTD.</param>
    /// <param name="input">The reader to load the DTD from.</param>
    /// <param name="subset"></param>
    /// <param name="proxy">The proxy server to use when loading resources.</param>
    /// <param name="nt">The <see cref="XmlNameTable"/> is NOT used.</param>
    /// <returns>A new <see cref="SgmlDtd"/> instance that encapsulates the DTD.</returns>
    [SuppressMessage("Microsoft.Reliability", "CA2000", Justification = "The entities created here are not temporary and should not be disposed here.")]
    public static SgmlDtd Parse(Uri baseUri, string name, TextReader input, string subset, string proxy, XmlNameTable nt)
    {
        SgmlDtd dtd = new SgmlDtd(name, nt);
        dtd.PushEntity(baseUri, new Entity(dtd.Name, baseUri, input, proxy));
        if (!string.IsNullOrEmpty(subset))
        {
            dtd.PushEntity(baseUri, new Entity(name, subset));
        }

        try
        {
            dtd.Parse();
        }
        catch (ApplicationException e)
        {
            throw new SgmlParseException(e.Message + dtd.m_current.Context());
        }

        return dtd;
    }

    /// <summary>
    /// Finds an entity in the DTD with the specified name.
    /// </summary>
    /// <param name="name">The name of the <see cref="Entity"/> to find.</param>
    /// <returns>The specified Entity from the DTD.</returns>
    public Entity FindEntity(string name)
    {
        Entity e;
        m_entities.TryGetValue(name, out e);
        return e;
    }

    /// <summary>
    /// Finds an element declaration in the DTD with the specified name.
    /// </summary>
    /// <param name="name">The name of the <see cref="ElementDecl"/> to find and return.</param>
    /// <returns>The <see cref="ElementDecl"/> matching the specified name.</returns>
    public ElementDecl FindElement(string name)
    {
        ElementDecl el;
        m_elements.TryGetValue(name.ToUpperInvariant(), out el);
        return el;
    }

    //-------------------------------- Parser -------------------------
    private void PushEntity(Uri baseUri, Entity e)
    {
        e.Open(m_current, baseUri);
        m_current = e;
        m_current.ReadChar();
    }

    private void PopEntity()
    {
        if (m_current != null) m_current.Close();
        if (m_current.Parent != null)
        {
            m_current = m_current.Parent;
        }
        else
        {
            m_current = null;
        }
    }

    private void Parse()
    {
        char ch = m_current.Lastchar;
        while (true)
        {
            switch (ch)
            {
                case Entity.EOF:
                    PopEntity();
                    if (m_current == null)
                        return;
                    ch = m_current.Lastchar;
                    break;
                case ' ':
                case '\n':
                case '\r':
                case '\t':
                    ch = m_current.ReadChar();
                    break;
                case '<':
                    ParseMarkup();
                    ch = m_current.ReadChar();
                    break;
                case '%':
                    Entity e = ParseParameterEntity(WhiteSpace);
                    try
                    {
                        PushEntity(m_current.ResolvedUri, e);
                    }
                    catch (Exception ex)
                    {
                        // BUG: need an error log.
                        Console.WriteLine(ex.Message + m_current.Context());
                    }
                    ch = m_current.Lastchar;
                    break;
                default:
                    m_current.Error("Unexpected character '{0}'", ch);
                    break;
            }
        }
    }

    void ParseMarkup()
    {
        char ch = m_current.ReadChar();
        if (ch != '!')
        {
            m_current.Error("Found '{0}', but expecing declaration starting with '<!'");
            return;
        }
        ch = m_current.ReadChar();
        if (ch == '-')
        {
            ch = m_current.ReadChar();
            if (ch != '-') m_current.Error("Expecting comment '<!--' but found {0}", ch);
            m_current.ScanToEnd(m_sb, "Comment", "-->");
        }
        else if (ch == '[')
        {
            ParseMarkedSection();
        }
        else
        {
            string token = m_current.ScanToken(m_sb, WhiteSpace, true);
            switch (token)
            {
                case "ENTITY":
                    ParseEntity();
                    break;
                case "ELEMENT":
                    ParseElementDecl();
                    break;
                case "ATTLIST":
                    ParseAttList();
                    break;
                default:
                    m_current.Error("Invalid declaration '<!{0}'.  Expecting 'ENTITY', 'ELEMENT' or 'ATTLIST'.", token);
                    break;
            }
        }
    }

    char ParseDeclComments()
    {
        char ch = m_current.Lastchar;
        while (ch == '-')
        {
            ch = ParseDeclComment(true);
        }
        return ch;
    }

    char ParseDeclComment(bool full)
    {
        // This method scans over a comment inside a markup declaration.
        char ch = m_current.ReadChar();
        if (full && ch != '-') m_current.Error("Expecting comment delimiter '--' but found {0}", ch);
        m_current.ScanToEnd(m_sb, "Markup Comment", "--");
        return m_current.SkipWhitespace();
    }

    void ParseMarkedSection()
    {
        // <![^ name [ ... ]]>
        m_current.ReadChar(); // move to next char.
        string name = ScanName("[");
        if (string.Equals(name, "INCLUDE", StringComparison.OrdinalIgnoreCase))
        {
            ParseIncludeSection();
        }
        else if (string.Equals(name, "IGNORE", StringComparison.OrdinalIgnoreCase))
        {
            ParseIgnoreSection();
        }
        else
        {
            m_current.Error("Unsupported marked section type '{0}'", name);
        }
    }

    [SuppressMessage("Microsoft.Performance", "CA1822", Justification = "This is not yet implemented and will use 'this' in the future.")]
    [SuppressMessage("Microsoft.Globalization", "CA1303", Justification = "The use of a literal here is only due to this not yet being implemented.")]
    private void ParseIncludeSection()
    {
        throw new NotImplementedException("Include Section");
    }

    void ParseIgnoreSection()
    {
        char ch = m_current.SkipWhitespace();
        if (ch != '[') m_current.Error("Expecting '[' but found {0}", ch);
        m_current.ScanToEnd(m_sb, "Conditional Section", "]]>");
    }

    string ScanName(string term)
    {
        // skip whitespace, scan name (which may be parameter entity reference
        // which is then expanded to a name)
        char ch = m_current.SkipWhitespace();
        if (ch == '%')
        {
            Entity e = ParseParameterEntity(term);
            ch = m_current.Lastchar;
            // bugbug - need to support external and nested parameter entities
            if (!e.IsInternal) throw new NotSupportedException("External parameter entity resolution");
            return e.Literal.Trim();
        }
        else
        {
            return m_current.ScanToken(m_sb, term, true);
        }
    }

    private Entity ParseParameterEntity(string term)
    {
        // almost the same as this.current.ScanToken, except we also terminate on ';'
        m_current.ReadChar();
        string name = m_current.ScanToken(m_sb, ";" + term, false);
        if (m_current.Lastchar == ';')
            m_current.ReadChar();
        Entity e = GetParameterEntity(name);
        return e;
    }

    private Entity GetParameterEntity(string name)
    {
        Entity e = null;
        m_pentities.TryGetValue(name, out e);
        if (e == null)
            m_current.Error("Reference to undefined parameter entity '{0}'", name);

        return e;
    }

    /// <summary>
    /// Returns a dictionary for looking up entities by their <see cref="Entity.Literal"/> value.
    /// </summary>
    /// <returns>A dictionary for looking up entities by their <see cref="Entity.Literal"/> value.</returns>
    [SuppressMessage("Microsoft.Design", "CA1024", Justification = "This method creates and copies a dictionary, so exposing it as a property is not appropriate.")]
    public Dictionary<string, Entity> GetEntitiesLiteralNameLookup()
    {
        Dictionary<string, Entity> hashtable = new Dictionary<string, Entity>();
        foreach (Entity entity in m_entities.Values)
            hashtable[entity.Literal] = entity;

        return hashtable;
    }

    private const string WhiteSpace = " \r\n\t";

    private void ParseEntity()
    {
        char ch = m_current.SkipWhitespace();
        bool pe = ch == '%';
        if (pe)
        {
            // parameter entity.
            m_current.ReadChar(); // move to next char
            ch = m_current.SkipWhitespace();
        }
        string name = m_current.ScanToken(m_sb, WhiteSpace, true);
        ch = m_current.SkipWhitespace();
        Entity e = null;
        if (ch == '"' || ch == '\'')
        {
            string literal = m_current.ScanLiteral(m_sb, ch);
            e = new Entity(name, literal);
        }
        else
        {
            string pubid = null;
            string extid = null;
            string tok = m_current.ScanToken(m_sb, WhiteSpace, true);
            if (Entity.IsLiteralType(tok))
            {
                ch = m_current.SkipWhitespace();
                string literal = m_current.ScanLiteral(m_sb, ch);
                e = new Entity(name, literal);
                e.SetLiteralType(tok);
            }
            else
            {
                extid = tok;
                if (string.Equals(extid, "PUBLIC", StringComparison.OrdinalIgnoreCase))
                {
                    ch = m_current.SkipWhitespace();
                    if (ch == '"' || ch == '\'')
                    {
                        pubid = m_current.ScanLiteral(m_sb, ch);
                    }
                    else
                    {
                        m_current.Error("Expecting public identifier literal but found '{0}'", ch);
                    }
                }
                else if (!string.Equals(extid, "SYSTEM", StringComparison.OrdinalIgnoreCase))
                {
                    m_current.Error("Invalid external identifier '{0}'.  Expecing 'PUBLIC' or 'SYSTEM'.", extid);
                }
                string uri = null;
                ch = m_current.SkipWhitespace();
                if (ch == '"' || ch == '\'')
                {
                    uri = m_current.ScanLiteral(m_sb, ch);
                }
                else if (ch != '>')
                {
                    m_current.Error("Expecting system identifier literal but found '{0}'", ch);
                }
                e = new Entity(name, pubid, uri, m_current.Proxy);
            }
        }
        ch = m_current.SkipWhitespace();
        if (ch == '-')
            ch = ParseDeclComments();
        if (ch != '>')
        {
            m_current.Error("Expecting end of entity declaration '>' but found '{0}'", ch);
        }
        if (pe)
            m_pentities.Add(e.Name, e);
        else
            m_entities.Add(e.Name, e);
    }

    private void ParseElementDecl()
    {
        char ch = m_current.SkipWhitespace();
        string[] names = ParseNameGroup(ch, true);
        ch = char.ToUpperInvariant(m_current.SkipWhitespace());
        bool sto = false;
        bool eto = false;
        if (ch == 'O' || ch == '-')
        {
            sto = ch == 'O'; // start tag optional?   
            m_current.ReadChar();
            ch = char.ToUpperInvariant(m_current.SkipWhitespace());
            if (ch == 'O' || ch == '-')
            {
                eto = ch == 'O'; // end tag optional? 
                ch = m_current.ReadChar();
            }
        }
        ch = m_current.SkipWhitespace();
        ContentModel cm = ParseContentModel(ch);
        ch = m_current.SkipWhitespace();

        string[] exclusions = null;
        string[] inclusions = null;

        if (ch == '-')
        {
            ch = m_current.ReadChar();
            if (ch == '(')
            {
                exclusions = ParseNameGroup(ch, true);
                ch = m_current.SkipWhitespace();
            }
            else if (ch == '-')
            {
                ch = ParseDeclComment(false);
            }
            else
            {
                m_current.Error("Invalid syntax at '{0}'", ch);
            }
        }

        if (ch == '-')
            ch = ParseDeclComments();

        if (ch == '+')
        {
            ch = m_current.ReadChar();
            if (ch != '(')
            {
                m_current.Error("Expecting inclusions name group", ch);
            }
            inclusions = ParseNameGroup(ch, true);
            ch = m_current.SkipWhitespace();
        }

        if (ch == '-')
            ch = ParseDeclComments();


        if (ch != '>')
        {
            m_current.Error("Expecting end of ELEMENT declaration '>' but found '{0}'", ch);
        }

        foreach (string name in names)
        {
            string atom = name.ToUpperInvariant();
            m_elements.Add(atom, new ElementDecl(atom, sto, eto, cm, inclusions, exclusions));
        }
    }

    static string ngterm = " \r\n\t|,)";
    string[] ParseNameGroup(char ch, bool nmtokens)
    {
        ArrayList names = new ArrayList();
        if (ch == '(')
        {
            ch = m_current.ReadChar();
            ch = m_current.SkipWhitespace();
            while (ch != ')')
            {
                // skip whitespace, scan name (which may be parameter entity reference
                // which is then expanded to a name)                    
                ch = m_current.SkipWhitespace();
                if (ch == '%')
                {
                    Entity e = ParseParameterEntity(ngterm);
                    PushEntity(m_current.ResolvedUri, e);
                    ParseNameList(names, nmtokens);
                    PopEntity();
                    ch = m_current.Lastchar;
                }
                else
                {
                    string token = m_current.ScanToken(m_sb, ngterm, nmtokens);
                    token = token.ToUpperInvariant();
                    names.Add(token);
                }
                ch = m_current.SkipWhitespace();
                if (ch == '|' || ch == ',') ch = m_current.ReadChar();
            }
            m_current.ReadChar(); // consume ')'
        }
        else
        {
            string name = m_current.ScanToken(m_sb, WhiteSpace, nmtokens);
            name = name.ToUpperInvariant();
            names.Add(name);
        }
        return (string[])names.ToArray(typeof(string));
    }

    void ParseNameList(ArrayList names, bool nmtokens)
    {
        char ch = m_current.Lastchar;
        ch = m_current.SkipWhitespace();
        while (ch != Entity.EOF)
        {
            string name;
            if (ch == '%')
            {
                Entity e = ParseParameterEntity(ngterm);
                PushEntity(m_current.ResolvedUri, e);
                ParseNameList(names, nmtokens);
                PopEntity();
                ch = m_current.Lastchar;
            }
            else
            {
                name = m_current.ScanToken(m_sb, ngterm, true);
                name = name.ToUpperInvariant();
                names.Add(name);
            }
            ch = m_current.SkipWhitespace();
            if (ch == '|')
            {
                ch = m_current.ReadChar();
                ch = m_current.SkipWhitespace();
            }
        }
    }

    static string dcterm = " \r\n\t>";
    private ContentModel ParseContentModel(char ch)
    {
        ContentModel cm = new ContentModel();
        if (ch == '(')
        {
            m_current.ReadChar();
            ParseModel(')', cm);
            ch = m_current.ReadChar();
            if (ch == '?' || ch == '+' || ch == '*')
            {
                cm.AddOccurrence(ch);
                m_current.ReadChar();
            }
        }
        else if (ch == '%')
        {
            Entity e = ParseParameterEntity(dcterm);
            PushEntity(m_current.ResolvedUri, e);
            cm = ParseContentModel(m_current.Lastchar);
            PopEntity(); // bugbug should be at EOF.
        }
        else
        {
            string dc = ScanName(dcterm);
            cm.SetDeclaredContent(dc);
        }
        return cm;
    }

    static string cmterm = " \r\n\t,&|()?+*";
    void ParseModel(char cmt, ContentModel cm)
    {
        // Called when part of the model is made up of the contents of a parameter entity
        int depth = cm.CurrentDepth;
        char ch = m_current.Lastchar;
        ch = m_current.SkipWhitespace();
        while (ch != cmt || cm.CurrentDepth > depth) // the entity must terminate while inside the content model.
        {
            if (ch == Entity.EOF)
            {
                m_current.Error("Content Model was not closed");
            }
            if (ch == '%')
            {
                Entity e = ParseParameterEntity(cmterm);
                PushEntity(m_current.ResolvedUri, e);
                ParseModel(Entity.EOF, cm);
                PopEntity();
                ch = m_current.SkipWhitespace();
            }
            else if (ch == '(')
            {
                cm.PushGroup();
                m_current.ReadChar();// consume '('
                ch = m_current.SkipWhitespace();
            }
            else if (ch == ')')
            {
                ch = m_current.ReadChar();// consume ')'
                if (ch == '*' || ch == '+' || ch == '?')
                {
                    cm.AddOccurrence(ch);
                    ch = m_current.ReadChar();
                }
                if (cm.PopGroup() < depth)
                {
                    m_current.Error("Parameter entity cannot close a paren outside it's own scope");
                }
                ch = m_current.SkipWhitespace();
            }
            else if (ch == ',' || ch == '|' || ch == '&')
            {
                cm.AddConnector(ch);
                m_current.ReadChar(); // skip connector
                ch = m_current.SkipWhitespace();
            }
            else
            {
                string token;
                if (ch == '#')
                {
                    ch = m_current.ReadChar();
                    token = "#" + m_current.ScanToken(m_sb, cmterm, true); // since '#' is not a valid name character.
                }
                else
                {
                    token = m_current.ScanToken(m_sb, cmterm, true);
                }

                token = token.ToUpperInvariant();
                ch = m_current.Lastchar;
                if (ch == '?' || ch == '+' || ch == '*')
                {
                    cm.PushGroup();
                    cm.AddSymbol(token);
                    cm.AddOccurrence(ch);
                    cm.PopGroup();
                    m_current.ReadChar(); // skip connector
                    ch = m_current.SkipWhitespace();
                }
                else
                {
                    cm.AddSymbol(token);
                    ch = m_current.SkipWhitespace();
                }
            }
        }
    }

    void ParseAttList()
    {
        char ch = m_current.SkipWhitespace();
        string[] names = ParseNameGroup(ch, true);
        Dictionary<string, AttDef> attlist = new Dictionary<string, AttDef>();
        ParseAttList(attlist, '>');
        foreach (string name in names)
        {
            ElementDecl e;
            if (!m_elements.TryGetValue(name, out e))
            {
                m_current.Error("ATTLIST references undefined ELEMENT {0}", name);
            }

            e.AddAttDefs(attlist);
        }
    }

    static string peterm = " \t\r\n>";
    void ParseAttList(Dictionary<string, AttDef> list, char term)
    {
        char ch = m_current.SkipWhitespace();
        while (ch != term)
        {
            if (ch == '%')
            {
                Entity e = ParseParameterEntity(peterm);
                PushEntity(m_current.ResolvedUri, e);
                ParseAttList(list, Entity.EOF);
                PopEntity();
                ch = m_current.SkipWhitespace();
            }
            else if (ch == '-')
            {
                ch = ParseDeclComments();
            }
            else
            {
                AttDef a = ParseAttDef(ch);
                list.Add(a.Name, a);
            }
            ch = m_current.SkipWhitespace();
        }
    }

    AttDef ParseAttDef(char ch)
    {
        ch = m_current.SkipWhitespace();
        string name = ScanName(WhiteSpace);
        name = name.ToUpperInvariant();
        AttDef attdef = new AttDef(name);

        ch = m_current.SkipWhitespace();
        if (ch == '-')
            ch = ParseDeclComments();

        ParseAttType(ch, attdef);

        ch = m_current.SkipWhitespace();
        if (ch == '-')
            ch = ParseDeclComments();

        ParseAttDefault(ch, attdef);

        ch = m_current.SkipWhitespace();
        if (ch == '-')
            ch = ParseDeclComments();

        return attdef;

    }

    void ParseAttType(char ch, AttDef attdef)
    {
        if (ch == '%')
        {
            Entity e = ParseParameterEntity(WhiteSpace);
            PushEntity(m_current.ResolvedUri, e);
            ParseAttType(m_current.Lastchar, attdef);
            PopEntity(); // bugbug - are we at the end of the entity?
            ch = m_current.Lastchar;
            return;
        }

        if (ch == '(')
        {
            //attdef.EnumValues = ParseNameGroup(ch, false);  
            //attdef.Type = AttributeType.ENUMERATION;
            attdef.SetEnumeratedType(ParseNameGroup(ch, false), AttributeType.ENUMERATION);
        }
        else
        {
            string token = ScanName(WhiteSpace);
            if (string.Equals(token, "NOTATION", StringComparison.OrdinalIgnoreCase))
            {
                ch = m_current.SkipWhitespace();
                if (ch != '(')
                {
                    m_current.Error("Expecting name group '(', but found '{0}'", ch);
                }
                //attdef.Type = AttributeType.NOTATION;
                //attdef.EnumValues = ParseNameGroup(ch, true);
                attdef.SetEnumeratedType(ParseNameGroup(ch, true), AttributeType.NOTATION);
            }
            else
            {
                attdef.SetType(token);
            }
        }
    }

    void ParseAttDefault(char ch, AttDef attdef)
    {
        if (ch == '%')
        {
            Entity e = ParseParameterEntity(WhiteSpace);
            PushEntity(m_current.ResolvedUri, e);
            ParseAttDefault(m_current.Lastchar, attdef);
            PopEntity(); // bugbug - are we at the end of the entity?
            ch = m_current.Lastchar;
            return;
        }

        bool hasdef = true;
        if (ch == '#')
        {
            m_current.ReadChar();
            string token = m_current.ScanToken(m_sb, WhiteSpace, true);
            hasdef = attdef.SetPresence(token);
            ch = m_current.SkipWhitespace();
        }
        if (hasdef)
        {
            if (ch == '\'' || ch == '"')
            {
                string lit = m_current.ScanLiteral(m_sb, ch);
                attdef.Default = lit;
                ch = m_current.SkipWhitespace();
            }
            else
            {
                string name = m_current.ScanToken(m_sb, WhiteSpace, false);
                name = name.ToUpperInvariant();
                attdef.Default = name; // bugbug - must be one of the enumerated names.
                ch = m_current.SkipWhitespace();
            }
        }
    }
}