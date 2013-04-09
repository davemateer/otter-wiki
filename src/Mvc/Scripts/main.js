$(document).ready(function () {
  $('#add-article-link').button({
    icons: {
      primary: "ui-icon-circle-plus"
    }
  });

  $('.edit-button').button({
    icons: {
      primary: "ui-icon-pencil"
    }
  });

  $('.history-button').button({
    icons: {
      primary: "ui-icon-clock"
    }
  });

  $('#search-form input.query-submit').button();
});