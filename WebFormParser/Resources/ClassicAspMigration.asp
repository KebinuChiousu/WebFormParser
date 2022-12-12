<!-- comment -->
<!-- multi
     line
     regex
-->
<%@ Page %>
<html>
<head>
<title>title</title>
</head>
<body>
<form>
<% 
HttpContext.Current.Response.Write("Test!");
%>
<table>
<tr><td>
<input <% if (readonly) { %>readonly<% } %> value=<% valueTest %> />
</td></tr>
<tr><td>
<select>
<option <% if (true) { %>value=ok<% } %>>waka <% getValue(); %> test</option>
</select>
</td></tr>
</table>
<div class="content">
    <% if (value) { %>
         <span>This is a test</span> 
    <% } %>
</div>
</form>
</body>
</html>