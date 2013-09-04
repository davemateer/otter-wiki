namespace Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Otter.Infrastructure;

    [TestClass]
    public class MarkdownConverterTest
    {
        [TestMethod]
        public void Convert_Table()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("{|\n|Orange\n|Apple\n|-\n|Bread\n|Pie\n|-\n|Butter\n|Ice Cream \n|}");
            string expected = "<table><tbody><tr><td><p>Orange</p></td><td><p>Apple</p></td></tr><tr><td><p>Bread</p></td><td><p>Pie</p></td></tr><tr><td><p>Butter</p></td><td><p>Ice Cream</p></td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_TableSingleLineRows()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("{|\n|Orange||Apple||more\n|-\n|Bread||Pie||more\n|-\n|Butter||Ice Cream||and more\n|}");
            string expected = "<table><tbody><tr><td>Orange</td><td>Apple</td><td>more</td></tr><tr><td>Bread</td><td>Pie</td><td>more</td></tr><tr><td>Butter</td><td>Ice Cream</td><td>and more</td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_TableIgnoreExtraSpacing()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("{|\n|  Orange    ||   Apple   ||   more\n|-\n|   Bread    ||   Pie     ||   more\n|-\n|   Butter   || Ice cream ||  and more\n|}");
            string expected = "<table><tbody><tr><td>Orange</td><td>Apple</td><td>more</td></tr><tr><td>Bread</td><td>Pie</td><td>more</td></tr><tr><td>Butter</td><td>Ice cream</td><td>and more</td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_TableLongText()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("{|\n|Lorem ipsum dolor sit amet,  \nconsetetur sadipscing elitr, \nsed diam nonumy eirmod tempor invidunt\nut labore et dolore magna aliquyam erat, \nsed diam voluptua. \n\nAt vero eos et accusam et justo duo dolores\net ea rebum. Stet clita kasd gubergren,\nno sea takimata sanctus est Lorem ipsum\ndolor sit amet. \n|\n- Lorem ipsum dolor sit amet\n- consetetur sadipscing elitr\n- sed diam nonumy eirmod tempor invidunt\n|}");
            string expected = "<table><tbody><tr><td><p>Lorem ipsum dolor sit amet,<br>\nconsetetur sadipscing elitr, \nsed diam nonumy eirmod tempor invidunt\nut labore et dolore magna aliquyam erat, \nsed diam voluptua. </p>\n\n<p>At vero eos et accusam et justo duo dolores\net ea rebum. Stet clita kasd gubergren,\nno sea takimata sanctus est Lorem ipsum\ndolor sit amet.</p></td><td><ul>\n<li>Lorem ipsum dolor sit amet</li>\n<li>consetetur sadipscing elitr</li>\n<li>sed diam nonumy eirmod tempor invidunt</li>\n</ul></td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_TableHeaders()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("{|\n! Item\n! Amount\n! Cost\n|-\n|Orange\n|10\n|7.00\n|-\n|Bread\n|4\n|3.00\n|-\n|Butter\n|1\n|5.00\n|-\n!Total\n|\n|15.00\n|}");
            string expected = "<table><thead><tr><th><p>Item</p></th><th><p>Amount</p></th><th><p>Cost</p></th></tr></thead><tbody><tr><td><p>Orange</p></td><td><p>10</p></td><td><p>7.00</p></td></tr><tr><td><p>Bread</p></td><td><p>4</p></td><td><p>3.00</p></td></tr><tr><td><p>Butter</p></td><td><p>1</p></td><td><p>5.00</p></td></tr><tr><th><p>Total</p></th><td>&nbsp;</td><td><p>15.00</p></td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_TableForbidInbalancedTables()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("{|\n|Orange||Apple||more\n|-\n|Bread||Pie||more\n|-\n|Butter||and more\n|}");
            string expected = "<p>{|\n|Orange||Apple||more\n|-\n|Bread||Pie||more\n|-\n|Butter||and more\n|}</p>";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Convert_MixedTable()
        {
            var converter = new MarkdownConverter();
            var actual = converter.Convert("Interface servers\n-----------------\n\n{|\n! Server !! Purpose\n|-\n| IF-HVB-03 || MCAP Dictation Interface\n|}");
            string expected = "<h2>Interface servers</h2>\n\n<table><thead><tr><th>Server</th><th>Purpose</th></tr></thead><tbody><tr><td>IF-HVB-03</td><td>MCAP Dictation Interface</td></tr></tbody></table>";
            Assert.AreEqual(expected, actual);
        }
    }
}
