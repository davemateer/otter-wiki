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
  // target.find('code').addClass("prettyprint");

  target.find('pre > code').each(function (index) {
    // $(this).addClass("prettyprint");
    $(this).attr('id', 'codeBlock-' + index);

    var selector = $('<button/>', {
      "type": 'button',
      "click": function () { selectCode('codeBlock-' + index); },
      "class": 'select-all btn btn-default btn-xs pull-right hidden-print',
      "text": 'Select'
    });

    $(this).parent().prepend(selector);
  });

  // prettyPrint();

  target.find('table').not(".otter-attachment").not(".otter-article-information").addClass('table table-condensed table-bordered');
};