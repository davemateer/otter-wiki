$(document).ready(function () {
  $('.add-button').button({
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

  $('input.query-submit, input.submit-button').button();
});