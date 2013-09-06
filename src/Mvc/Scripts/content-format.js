var selectCode = function (elementId) {
  if (document.body.createTextRange) {
    var range = document.body.createTextRange();
    range.moveToElementText(document.getElementById(elementId));
    range.select();
  } else if (window.getSelection) {
    var selection = window.getSelection();
    var range = document.createRange();
    range.selectNodeContents(document.getElementById(elementId));
    selection.removeAllRanges();
    selection.addRange(range);
  }
};

var applyContentFormat = function (target) {

  target.find('code').addClass("prettyprint");

  target.find('pre > code').parent().each(function (index) {
    $(this).addClass("prettyprint");
    $(this).attr('id', 'codeblock-' + index);
    $(this).after($('<a/>', {
      href: "javascript:selectCode('codeblock-" + index + "')",
      text: "Select code",
      "class": "select-all"
    }))
  });

  prettyPrint();

  target.find('table').addClass('content-table');
};
