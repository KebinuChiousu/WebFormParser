using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using WebFormParser.model;
using WebFormParser.Utility;

namespace WebFormParser.csp
{
    public static class CleanJs
    {
        public static IDocument JsParser(IDocument document, string filename)
        {

            string js = "";
            string addVariablesToOnload = "";
            string aspTag = "ASP:".ToLower();
            List<string[]> createdFunctionsNames = new List<string[]>();
            Source.anonymouseFunctions = new List<string>();
            var jsTags = document.GetElementsByTagName("script");
            foreach (var jsTagElement in jsTags)
            {
                var jsTag = jsTagElement;
                //js += styleTag.InnerHtml + "\n";
                if (jsTag.GetAttribute("src") != null)
                {
                    //*************** START move src to HEAD ************************************
                    //jsTag = SetJsSrcTag(jsTag, ref document);
                    //*************** STOP move src to HEAD ************************************
                }
                else
                {

                    //*************** START get all valid functions ************************************
                    jsTag = SetJsFunctions(jsTag, ref js);
                    //*************** END get all valid functions ************************************

                    //*************** START get variables ********************************************
                    jsTag = SetJsVariables(jsTag, ref js, ref addVariablesToOnload, ref document);
                    //*************** END get variables **********************************************

                    //*************** START Embedded Dynamic Elements **********************************************
                    //jsTag = SetEmbeddedDynamicElements(jsTag, ref js, ref addVariablesToOnload, ref document);
                    //*************** END Embedded Dynamic Elements **********************************************

                    if (!jsTag.InnerHtml.Contains("<%="))
                    {
                        if (jsTag.InnerHtml.Contains("addEventListener"))
                        {
                            Source.error += "Error moving addEventListener to " + filename;
                        }
                        else
                        {
                            //Source.embeddedJsScript += removeJsScriptFromFile(jsTag);
                        }
                        //Source.embeddedJsScript += removeJsScriptFromFile(jsTag);
                    }
                    else
                    {
                        Source.error += "Cannot move script from " + filename + "\n";
                    }
                }

            }

            var elements = document.All;
            foreach (var elementItem in elements)
            {
                var element = elementItem;

                bool isASP = element.TagName.Length < 4 ? false : (element.TagName.Substring(0, 4).ToLower() == aspTag.ToLower());
                //*************** START Js EventActions **********************************************
                element = handleJsEventActions(element, ref createdFunctionsNames, ref isASP, ref js);
                //*************** START Js EventActions **********************************************

                //Start of nonFunctioningEvents
                string[] nonFunctioningEvents = new string[] { "onbeforesubmit", "onmousewheel" };
                foreach (string nonFunctioningEvent in nonFunctioningEvents)
                {
                    var eventString = element.GetAttribute(nonFunctioningEvent);
                    if (eventString != null && !isASP)
                    {
                        bool containsASP = eventString.Contains("<%");
                        if (!containsASP)
                        {
                            Source.source = Util.ReplaceFirst(Source.source, nonFunctioningEvent + "=\"" + eventString + "\"", "");
                            element.RemoveAttribute(nonFunctioningEvent);
                            Source.changed = true;
                        }
                        else
                        {
                            Source.error += "Manual review need for " + filename + "\n";
                        }
                    }
                } // End loop nonFunctioningEvents

                //ASP Events
                string aspEvent = "OnClientClick";
                var eventStringASP = element.GetAttribute(aspEvent);
                if (eventStringASP != null && isASP)
                {
                    bool containsASP = eventStringASP.Contains("<%");
                    if (!containsASP)
                    {
                        var id = element.GetAttribute("id");
                        if (id == null)
                        {
                            Guid myuuid = Guid.NewGuid();
                            id = "randomId-" + myuuid.ToString();
                            id = id.Replace("-", "");
                            Source.source = Util.ReplaceFirst(Source.source, aspEvent + "=\"" + eventStringASP + "\"", "id=\"" + id + "\"");
                        }
                        else
                        {
                            Source.source = Util.ReplaceFirst(Source.source, aspEvent + "=\"" + eventStringASP + "\"", "");
                        }

                        element.SetAttribute("id", id);
                        element.RemoveAttribute(aspEvent);
                        string shortId = Util.RandomString(4);
                        string functionName = id + aspEvent + shortId + "parser";
                        createdFunctionsNames.Add(new string[] { functionName, id, "click" });
                        js += "function " + functionName + "() {" + eventStringASP + "}\n";
                        Source.changed = true;
                    }
                    else
                    {
                        Source.error += "Manual review need for " + filename + "\n";
                    }
                }

            } // End loop for elements loop


            //Write JS to file
            if (js != "")
            {
                var jsFilename = filename + ".js";
                //Get old js and add it to the js variable, so that it can be written back
                //Otherwise the old js will get overwritten by File.Create(); -MH 2022
                if (File.Exists(jsFilename))
                {
                    string oldCSS = File.ReadAllText(jsFilename);
                    js = oldCSS + "\n" + js;
                }

                string loadFunctionName = "onLoad" + Util.RandomString(8) + "Function";
                while (js.Contains(loadFunctionName))
                {
                    loadFunctionName = "onLoad" + Util.RandomString(8) + "Function";
                }

                js += "function " + loadFunctionName + "(){\n";
                js += addVariablesToOnload + "\n";
                js += "\tvar element;\n";
                foreach (string[] createdfunctionName in createdFunctionsNames)
                {
                    js += "\telement = document.getElementById(\"" + createdfunctionName[1] + "\");\n ";
                    js += "\tif(element != null){ \n";
                    js += "\t\telement.addEventListener('" + createdfunctionName[2] + "', " + createdfunctionName[0] + "); \n";
                    js += "\t}\n";
                }
                string[] embeddedJsScriptLines = Source.embeddedJsScript.Split("\n");
                foreach (string embeddedJsScriptLine in embeddedJsScriptLines)
                {
                    js += "\t" + embeddedJsScriptLine + "\n";
                }
                js += "}\n\n";

                //Add load Function to page load event.
                js += "window.addEventListener('load', " + loadFunctionName + ");\n";

                //Create file if it doesn't exist, else overwrite file.
                using (FileStream fs = File.Create(jsFilename))
                {
                    byte[] info = new UTF8Encoding(true).GetBytes(js);
                    fs.Write(info, 0, info.Length);
                }
                var jsLinkTag = document.CreateElement<AngleSharp.Html.Dom.IHtmlScriptElement>();
                jsLinkTag.SetAttribute("src", Path.GetFileName(jsFilename));
                jsLinkTag.SetAttribute("type", "text/javascript");
                if (!Source.source.Contains(jsFilename))
                {
                    Util.addToHead(jsLinkTag.OuterHtml, jsFilename);
                }
            }


            return document;
        }

        private static IElement SetEmbeddedDynamicElements(IElement jsTag, ref string js, ref string addVariablesToOnload, ref IDocument document)
        {
            String regexToGetDynamicJS = "<%=(.*)%>";
            MatchCollection dynamicJsElements = Regex.Matches(jsTag.InnerHtml, regexToGetDynamicJS);
            String[] aspVarNames = new String[] { "_sContent", "_sScript" };
            //_sRiskTable _sScriptLast _sContentPhone _sContentEmail _sItemListContent _sApprovalContent _sAltItemListContent _sDeliverableListContent _sBOAAltDeliverableListContent _sDocumentListContent _sEditorListContent _sSSRListContent
            foreach (Match dynamicJsElement in dynamicJsElements)
            {
                String apsElement = dynamicJsElement.Value;
                String aspVarName = String.Empty;
                bool ableToModify = false;
                if (dynamicJsElement.Groups.Count > 1)
                {
                    aspVarName = dynamicJsElement.Groups[1].Value.Trim();
                }
                foreach (string line in Source.source.Split("\n"))
                {
                    if (line.Trim() == apsElement.Trim())
                    {
                        ableToModify = true;
                    }
                }
                if (!ableToModify)
                {
                    Source.error += "Do not have method for " + aspVarName + " in " + Source.filename + "\n";
                }
                else
                {
                    string aspFunction = "jsParserInJs(\"" + aspVarName + "\");";
                    jsTag.InnerHtml = Util.ReplaceFirst(jsTag.InnerHtml, apsElement, aspFunction);
                    Source.source = Util.ReplaceFirst(Source.source, apsElement, aspFunction);
                    Source.changed = true;
                    Source.addJsParserToHeader = true;

                    String outerhtml = "<div id=\"" + aspVarName + "\" class=\"displayNone\"><%= " + aspVarName + " %></div>";
                    bool addedSuccessful = Util.AddHiddenElementToBody(outerhtml, Source.filename);
                }
            }
            if (Source.addJsParserToHeader)
            {
                Source.addJsParserDotJs = true;
                string parserFilename = "jsParserInJs.js";
                var jsLinkTag = document.CreateElement<AngleSharp.Html.Dom.IHtmlScriptElement>();
                jsLinkTag.SetAttribute("src", Path.GetFileName(parserFilename));
                jsLinkTag.SetAttribute("type", "text/javascript");
                if (!Source.source.Contains(parserFilename))
                {
                    Util.addToHead(jsLinkTag.OuterHtml, parserFilename);
                }
                Source.addJsParserToHeader = false;

            }
            return jsTag;
        }

        private static IElement SetJsVariables(IElement jsTag, ref string js, ref string addVariablesToOnload, ref IDocument document)
        {
            String regexToGetvariableStrings = @"var (.*);";
            MatchCollection varStatements = Regex.Matches(jsTag.InnerHtml, regexToGetvariableStrings);
            foreach (Match varStatement in varStatements)
            {
                String regexToGetAsp = "\"<%=(.*)%>\"";
                Match aspCode = Regex.Match(varStatement.Value, regexToGetAsp);
                bool isInAnonymous = isInAnonymousFunction(varStatement);
                if (!aspCode.Success)
                {
                    regexToGetAsp = "'<%=(.*)%>'";
                    aspCode = Regex.Match(varStatement.Value, regexToGetAsp);
                };
                if (!aspCode.Success)
                {
                    regexToGetAsp = "<%=(.*)%>";
                    aspCode = Regex.Match(varStatement.Value, regexToGetAsp);
                    if (aspCode.Success)
                    {
                        Console.WriteLine("TODO: Fix issue with booleans in asp tags");
                        //TODO: Fix issue with booleans in asp tags
                        continue;
                    }
                };
                string[] split = varStatement.Value.Split('=');
                if (aspCode.Success && split != null && split.Length > 0)
                {
                    string id = split[0].Replace("var", "").Trim();
                    string getElementValue = "document.getElementById('" + id + "').value";
                    string remainder = String.Join("=", split.Skip(1).ToArray());
                    remainder = Regex.Replace(remainder, regexToGetAsp, getElementValue);
                    var inputElement = document.CreateElement<AngleSharp.Html.Dom.IHtmlInputElement>();
                    inputElement.SetAttribute("id", id);
                    inputElement.SetAttribute("class", "displayNone");
                    inputElement.SetAttribute("type", "text");
                    string substring = "";
                    if (aspCode.Value.StartsWith("\"") || aspCode.Value.StartsWith("'"))
                    {
                        substring = aspCode.Value.Substring(1, aspCode.Value.Length - 2);
                    }
                    else
                    {
                        substring = aspCode.Value;
                    }
                    inputElement.SetAttribute("value", substring);
                    bool addedSuccessful = Util.AddHiddenElementToBody(inputElement.OuterHtml, Source.filename);
                    if (addedSuccessful)
                    {
                        js = "var " + id + ";" + "\n" + js;
                        //TODO fix this
                        //addVariablesToOnload += "\t" + id + " =" + remainder + "\n";
                        jsTag.InnerHtml = Util.ReplaceFirst(jsTag.InnerHtml, varStatement.Value, id + " =" + remainder);
                        Source.source = Util.ReplaceFirst(Source.source, varStatement.Value, id + " =" + remainder);
                        Source.changed = true;
                    }
                }
                else if (split != null && split.Length > 0 && !isInAnonymous)
                {
                    string id = split[0].Replace("var", "").Trim();
                    string remainder = String.Join("=", split.Skip(1).ToArray());
                    //TODO fix this
                    //js = "var " + id + ";" + "\n" + js;
                    //addVariablesToOnload += "\t" + id + " =" + remainder + "\n";
                    jsTag.InnerHtml = Util.ReplaceFirst(jsTag.InnerHtml, varStatement.Value, id + " =" + remainder);
                    Source.source = Util.ReplaceFirst(Source.source, varStatement.Value, id + " =" + remainder);
                    Source.changed = true;
                }
                else if (!isInAnonymous)
                {
                    js = varStatement + "\n" + js;
                    jsTag.InnerHtml = Util.ReplaceFirst(jsTag.InnerHtml, varStatement.Value, String.Empty);
                    Source.source = Util.ReplaceFirst(Source.source, varStatement.Value, String.Empty);
                    Source.changed = true;

                }
            }
            return jsTag;
        }

        private static bool isInAnonymousFunction(Match varStatement)
        {
            foreach (string function in Source.anonymouseFunctions)
            {
                if (function.Contains(varStatement.ToString()))
                {
                    return true;
                }
            }
            return false;
        }
        private static IElement SetJsFunctions(IElement jsTag, ref string js)
        {

            String regexToGetFunctionNames = @"function (.*)\(";
            MatchCollection functionNames = Regex.Matches(jsTag.InnerHtml, regexToGetFunctionNames);
            string tempForAnonymousFunctions = jsTag.InnerHtml;
            foreach (Match functionName in functionNames)
            {
                if (functionName.ToString().Contains(")") || functionName.ToString().Trim() == "")
                {
                    continue;
                }
                else if (functionName.ToString().Trim().Contains("function ("))
                {
                    string anonFunction = GetFunction(tempForAnonymousFunctions, functionName.ToString().Replace("(", "").Replace("function ", "").Trim());
                    if (anonFunction != tempForAnonymousFunctions)
                    {
                        tempForAnonymousFunctions = tempForAnonymousFunctions.Replace(anonFunction, "");
                        Source.anonymouseFunctions.Add(anonFunction);
                    }
                    continue;
                }
                string functionAsString = GetFunction(jsTag.InnerHtml, functionName.ToString().Replace("(", "").Replace("function ", "").Trim());
                if (functionAsString.Contains("<%--") && functionAsString.Contains("--%>"))
                {
                    String commentsRegex = @"<%--[\s\S]*--%>";
                    MatchCollection commentedCodes = Regex.Matches(functionAsString, commentsRegex);
                    foreach (Match commentedCode in commentedCodes)
                    {
                        string comment = commentedCode.ToString();
                        string replacementComment = comment.Replace("<%--", "/*").Replace("--%>", "*/");
                        functionAsString = functionAsString.Replace(comment, replacementComment);
                        Source.source = Source.source.Replace(comment, replacementComment);
                        jsTag.InnerHtml = jsTag.InnerHtml.Replace(comment, replacementComment);
                    }
                }
                if (functionAsString.Contains("<%"))
                {
                    Source.error += "ERROR with JS Function for " + Source.filename + "\n";
                    continue;
                }
                js += functionAsString + "\n";
                jsTag.InnerHtml = Util.ReplaceFirst(jsTag.InnerHtml, functionAsString, String.Empty);
                string sourceFunctionAsString = GetFunction(Source.source, functionName.ToString().Replace("(", "").Replace("function ", "").Trim());
                Source.source = Util.ReplaceFirst(Source.source, sourceFunctionAsString, String.Empty);
                Source.changed = true;
            }
            return jsTag;
        }

        private static IElement? SetJsSrcTag(IElement jsTag, ref IDocument document)
        {

            string nodeName = "HEAD";
            if (jsTag != null && jsTag.Parent != null)
            {
                nodeName = jsTag.Parent.NodeName;
            }
            if (nodeName.ToLower() != "HEAD".ToLower())
            {
                bool removedSuccessful = JsRemoveFromSource(jsTag);
                if (removedSuccessful)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
                    jsTag.Parent.RemoveChild(jsTag);
#pragma warning restore CS8604 // Possible null reference argument.
                    bool addedSuccessful = Util.addToHead(jsTag.OuterHtml, Source.filename);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    if (addedSuccessful)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        document.Head.Append(jsTag);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                }
            }
            return jsTag;
        }

        private static string removeJsScriptFromFile(IElement jsTag)
        {

            List<string> newTag = new List<string>();
            string[] tagLines = jsTag.OuterHtml.Split("\n");
            bool isWithinScriptOuterHTML = false;
            for (int i = 0; i < tagLines.Length; i++)
            {
                if (tagLines[i].Trim() != "")
                {
                    newTag.Add(tagLines[i].Trim());
                }
            }
            string tagFirstLine = newTag[0].Trim();
            string tagLastLine = newTag[newTag.Count - 1].Trim();

            string[] lines = Source.source.Split("\n");
            List<string> newCode = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim() == tagFirstLine)
                {
                    isWithinScriptOuterHTML = true;
                }
                if (isWithinScriptOuterHTML)
                {
                    if (lines[i].Trim() != "")
                    {
                        newCode.Add(lines[i].Trim());
                    }
                }
                else
                {
                    newCode.Add(lines[i]);
                }
                if (lines[i].Trim() == tagLastLine)
                {
                    isWithinScriptOuterHTML = false;
                }
            }
            Source.source = String.Join("\n", newCode);
            string outerHTML = String.Join("\n", newTag);
            Source.source = Source.source.Replace(outerHTML, "");
            outerHTML = outerHTML.Replace(tagFirstLine, string.Empty);
            outerHTML = outerHTML.Replace(tagLastLine, string.Empty);
            return outerHTML;
        }

        private static bool JsRemoveFromSource(IElement? jsTag)
        {
            return Util.RemoveFromSource(jsTag, @"<script[^>]*>[\s\S]*?</script>", "src");
        }

        private static IElement handleJsEventActions(IElement element, ref List<string[]> createdFunctionsNames, ref bool isASP, ref string js)
        {

            string[] eventActions = new string[] { "onclick", "onblur", "onchange", "onselect", "onsubmit", "onfocus", "oninput", "onload",
                                                        "onkeydown", "onkeypress", "onkeyup", "ondblclick", "onmousedown", "onmousemove",
                                                        "onmouseout", "onmousein", "onmouseover", "onmouseup", "onwheel", "ontoggle"
                };
            foreach (string eventActionItem in eventActions)
            {
                var eventAction = eventActionItem;
                var eventString = element.GetAttribute(eventAction);
                if (eventString != null && !isASP)
                {
                    bool containsASP = eventString.Contains("<%");
                    string eventFullString = eventAction + "=\"" + eventString + "\"";
                    if (!Source.source.Contains(eventFullString))
                    {
                        eventFullString = eventAction + "=\'" + eventString + "\'";
                    }
                    if (!containsASP)
                    {
                        var id = element.GetAttribute("id");
                        if (id == null)
                        {
                            Guid myuuid = Guid.NewGuid();
                            id = "randomId-" + myuuid.ToString();
                            id = id.Replace("-", "");
                            Source.source = Util.ReplaceFirst(Source.source, eventFullString, "id=\"" + id + "\"");
                        }
                        else
                        {
                            Source.source = Util.ReplaceFirst(Source.source, eventFullString, "");
                        }

                        element.SetAttribute("id", id);
                        element.RemoveAttribute(eventAction);
                        string shortId = Util.RandomString(4);
                        string functionName = id + eventAction + shortId + "parser";
                        createdFunctionsNames.Add(new string[] { functionName, id, eventAction[2..] });
                        js += "function " + functionName + "() {" + eventString + "}\n";
                        Source.changed = true;
                    }
                    else
                    {
                        Source.error += "Manual review need for " + Source.filename + "\n";
                    }
                }
            }
            return element;
        }

        private static string GetFunction(string jstext, string functionname)
        {
            var start = Regex.Match(jstext, @"function\s+" + functionname + @"\s*\([^)]*\)\s*{");

            if (!start.Success && functionname.Trim() != "")
            {
                Source.error += "Function not found: " + functionname + " for " + Source.filename + "\n";
            }

            StringBuilder sb = new StringBuilder(start.Value);
            jstext = jstext.Substring(start.Index + start.Value.Length);
            var brackets = 1;
            var i = 0;

            var delimiters = "`'\"";
            string? currentDelimiter = null;

            var isEscape = false;
            var isComment = false;
            var isMultilineComment = false;

            while (brackets > 0 && i < jstext.Length)
            {
                var c = jstext[i].ToString();
                var wasEscape = isEscape;

                if (isComment || !isEscape)
                {
                    if (c == @"\")
                    {
                        // Found escape symbol.
                        isEscape = true;
                    }
                    else if (i > 0 && !isComment && (c == "*" || c == "/") && jstext[i - 1] == '/')
                    {
                        // Found start of a comment block
                        isComment = true;
                        isMultilineComment = c == "*";
                    }
                    else if (c == "\n" && isComment && !isMultilineComment)
                    {
                        // Found termination of singline line comment
                        isComment = false;
                    }
                    else if (isMultilineComment && c == "/" && jstext[i - 1] == '*')
                    {
                        // Found termination of multiline comment
                        isComment = false;
                        isMultilineComment = false;
                    }
                    else if (delimiters.Contains(c))
                    {
                        // Found a string or regex delimiter
                        currentDelimiter = (currentDelimiter == c) ? null : currentDelimiter ?? c;
                    }

                    // The current symbol doesn't appear to be commented out, escaped or in a string
                    // If it is a bracket, we should treat it as one
                    if (currentDelimiter == null && !isComment)
                    {
                        if (c == "{")
                        {
                            brackets++;
                        }
                        if (c == "}")
                        {
                            brackets--;
                        }
                    }

                }

                sb.Append(c);
                i++;

                if (wasEscape) isEscape = false;
            }


            return sb.ToString();
        }

    }
}
