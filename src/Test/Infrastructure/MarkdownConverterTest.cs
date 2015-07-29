namespace Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Otter;

    [TestClass]
    public class MarkdownConverterTest
    {
        [TestMethod]
        public void Convert_Image()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("![sample](_img/sample.png \"Sample\")", "fooArticle");
            string expected = "<p><img src=\"%ARTICLE_IMAGES%/fooArticle/sample.png\" alt=\"sample\" title=\"Sample\"></p>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_ImageAndTable()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("![sample](_img/sample.png \"Sample\")\n\n{|\n|Orange\n|Apple\n|-\n|Bread\n|Pie\n|-\n|Butter\n|Ice Cream \n|}", "fooArticle");
            string expected = "<p><img src=\"%ARTICLE_IMAGES%/fooArticle/sample.png\" alt=\"sample\" title=\"Sample\"></p>\r\n<table><tbody><tr><td><p>Orange</p></td><td><p>Apple</p></td></tr><tr><td><p>Bread</p></td><td><p>Pie</p></td></tr><tr><td><p>Butter</p></td><td><p>Ice Cream</p></td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_MixedTable()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("Interface servers\n-----------------\n\n{|\n! Server !! Purpose\n|-\n| IF-HVB-03 || MCAP Dictation Interface\n|}", null);
            string expected = "<h2>Interface servers</h2>\r\n<table><thead><tr><th>Server</th><th>Purpose</th></tr></thead><tbody><tr><td>IF-HVB-03</td><td>MCAP Dictation Interface</td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_MultipleTables()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("Table 1\n-------\n{|\n| Apple || Fruit\n|}\n\nTable 2\n-------\n{|\n| Carrot || Vegetable\n|}", null);
            string expected = "<h2>Table 1</h2>\r\n<table><tbody><tr><td>Apple</td><td>Fruit</td></tr></tbody></table>\r\n<h2>Table 2</h2>\r\n<table><tbody><tr><td>Carrot</td><td>Vegetable</td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_Table()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("{|\n|Orange\n|Apple\n|-\n|Bread\n|Pie\n|-\n|Butter\n|Ice Cream \n|}", null);
            string expected = "<table><tbody><tr><td><p>Orange</p></td><td><p>Apple</p></td></tr><tr><td><p>Bread</p></td><td><p>Pie</p></td></tr><tr><td><p>Butter</p></td><td><p>Ice Cream</p></td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_TableForbidInbalancedTables()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("{|\r\n|Orange||Apple||more\r\n|-\r\n|Bread||Pie||more\r\n|-\r\n|Butter||and more\r\n|}", null);
            string expected = "<p>{|\r\n|Orange||Apple||more\r\n|-\r\n|Bread||Pie||more\r\n|-\r\n|Butter||and more\r\n|}</p>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_TableHeaders()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("{|\n! Item\n! Amount\n! Cost\n|-\n|Orange\n|10\n|7.00\n|-\n|Bread\n|4\n|3.00\n|-\n|Butter\n|1\n|5.00\n|-\n!Total\n|\n|15.00\n|}", null);
            string expected = "<table><thead><tr><th><p>Item</p></th><th><p>Amount</p></th><th><p>Cost</p></th></tr></thead><tbody><tr><td><p>Orange</p></td><td><p>10</p></td><td><p>7.00</p></td></tr><tr><td><p>Bread</p></td><td><p>4</p></td><td><p>3.00</p></td></tr><tr><td><p>Butter</p></td><td><p>1</p></td><td><p>5.00</p></td></tr><tr><th><p>Total</p></th><td>&nbsp;</td><td><p>15.00</p></td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_TableIgnoreExtraSpacing()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("{|\n|  Orange    ||   Apple   ||   more\n|-\n|   Bread    ||   Pie     ||   more\n|-\n|   Butter   || Ice cream ||  and more\n|}", null);
            string expected = "<table><tbody><tr><td>Orange</td><td>Apple</td><td>more</td></tr><tr><td>Bread</td><td>Pie</td><td>more</td></tr><tr><td>Butter</td><td>Ice cream</td><td>and more</td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_TableLongText()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("{|\r\n|Lorem ipsum dolor sit amet,  \r\nconsetetur sadipscing elitr, \r\nsed diam nonumy eirmod tempor invidunt\r\nut labore et dolore magna aliquyam erat, \r\nsed diam voluptua. \r\n\r\nAt vero eos et accusam et justo duo dolores\net ea rebum. Stet clita kasd gubergren,\r\nno sea takimata sanctus est Lorem ipsum\r\ndolor sit amet. \r\n|\r\n- Lorem ipsum dolor sit amet\r\n- consetetur sadipscing elitr\r\n- sed diam nonumy eirmod tempor invidunt\r\n|}", null);
            string expected = "<table><tbody><tr><td><p>Lorem ipsum dolor sit amet,<br>\r\nconsetetur sadipscing elitr,\r\nsed diam nonumy eirmod tempor invidunt\r\nut labore et dolore magna aliquyam erat,\r\nsed diam voluptua.</p>\r\n<p>At vero eos et accusam et justo duo dolores\r\net ea rebum. Stet clita kasd gubergren,\r\nno sea takimata sanctus est Lorem ipsum\r\ndolor sit amet.</p></td><td><ul>\r\n<li>Lorem ipsum dolor sit amet</li>\r\n<li>consetetur sadipscing elitr</li>\r\n<li>sed diam nonumy eirmod tempor invidunt</li>\r\n</ul></td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_TableSingleLineRows()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("{|\n|Orange||Apple||more\n|-\n|Bread||Pie||more\n|-\n|Butter||Ice Cream||and more\n|}", null);
            string expected = "<table><tbody><tr><td>Orange</td><td>Apple</td><td>more</td></tr><tr><td>Bread</td><td>Pie</td><td>more</td></tr><tr><td>Butter</td><td>Ice Cream</td><td>and more</td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }
    }
}