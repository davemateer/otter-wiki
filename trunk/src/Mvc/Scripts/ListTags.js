var currentColumnCount = 1;
var currentSort = 'name';
var tags = null;

var layoutTags = function (sort) {

  // Calculate the ideal number of columns based on the current width.
  var idealColumns = Math.min(OTTER_MAX_COLUMNS, Math.floor($('#tag-list').width() / OTTER_MIN_WIDTH_PER_COLUMN));

  // If the number of columns has changed, or the sort order has changed, re-layout the items.
  if (idealColumns !== currentColumnCount || sort !== currentSort) {

    $('#tag-list').empty();

    var itemsPerColumn = Math.ceil(tags.length / idealColumns);
    var columnWidth = 100 / idealColumns;

    // Sort either by name (ASC) or count (DESC)
    var sorted = tags.sort(function (a, b) {
      return sort === 'count' ? b.count - a.count : (a.name.toLowerCase() < b.name.toLowerCase() ? -1 : 1)
    });

    $('.otter-sort').removeClass('active');
    $('#otter-sort-' + sort).addClass('active');

    for (var c = 0; c < idealColumns; c++) {
      // Create a new unordered list for this column.
      var ul = $('<ul/>', { width: columnWidth + '%' });
      for (var i = c * itemsPerColumn; i < (c + 1) * itemsPerColumn && i < sorted.length; i++) {
        ul.append(sorted[i].object);
      }

      // Add the list to the main content area.
      ul.appendTo($('#tag-list'));
    }

    currentColumnCount = idealColumns;
    currentSort = sort;
  }
}

var createSortButton = function (sort) {
  var button = $('<button/>', {
    "id": 'otter-sort-' + sort,
    "class": 'btn btn-default otter-sort',
    "click": function () { layoutTags(sort); }
  })
  .append($('<span/>', { "class": 'glyphicon glyphicon-sort' }))
  .append(' Sort by ' + sort);
  return button;
};

$(document).ready(function () {

  // Map the list items to an object array.
  tags = $('#tag-list li').map(function () {
    var tag = new Object();
    tag.object = $(this).clone();
    tag.object.find('span.tag-count').addClass('pull-right');
    tag.name = $(this).find('span.tag-name').text();
    tag.count = parseInt($(this).find('span.tag-count').text());
    return tag;
  });

  $('#sort-links').append(createSortButton('name'));
  $('#sort-links').append(createSortButton('count'));

  layoutTags('name');

  $(window).resize(function () {
    layoutTags(currentSort)
  });

});