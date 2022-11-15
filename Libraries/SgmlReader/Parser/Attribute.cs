namespace Sgml;

/// <summary>
/// This class represents an attribute.  The AttDef is assigned
/// from a validation process, and is used to provide default values.
/// </summary>
internal class Attribute
{
    internal string Name;    // the atomized name.
    internal AttDef DtdType; // the AttDef of the attribute from the SGML DTD.
    internal char QuoteChar; // the quote character used for the attribute value.
    private string m_literalValue; // the attribute value

    /// <summary>
    /// Attribute objects are reused during parsing to reduce memory allocations, 
    /// hence the Reset method.
    /// </summary>
    public void Reset(string name, string value, char quote)
    {
        Name = name;
        m_literalValue = value;
        QuoteChar = quote;
        DtdType = null;
    }

    public string Value
    {
        get
        {
            if (m_literalValue != null)
                return m_literalValue;
            if (DtdType != null)
                return DtdType.Default;
            return null;
        }
        /*            set
                    {
                        this.m_literalValue = value;
                    }*/
    }

    public bool IsDefault
    {
        get
        {
            return m_literalValue == null;
        }
    }
}