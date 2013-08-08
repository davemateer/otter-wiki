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

  $('.cancel-button').button({
    icons: {
      primary: "ui-icon-cancel"
    }
  });

  $('.list-button').button({
    icons: {
      primary: "ui-icon-tag"
    }
  });

  $('input.query-submit, input.submit-button').button();

  $('input.permission-option').change(function () {
    $(this).siblings('input.permission-accounts').prop('disabled', !($(this).hasClass('specified')));
  });

  function split(val) {
    return val.split(/;\s*/);
  }

  function extractLast(term) {
    return split(term).pop();
  }

  $("input.permission-accounts")
    .bind("keydown", function (event) {
      if (event.keyCode === $.ui.keyCode.TAB && $(this).data("ui-autocomplete").menu.active) {
        event.preventDefault();
      }
    })
    .autocomplete({
      source: function (request, response) {
        $.getJSON(SITE_ROOT + "Security/SearchUsersAndGroups", {
          query: extractLast(request.term)
        }, response);
      },
      search: function () {
        var term = extractLast(this.value);
        if (term.length < 3) {
          return false;
        }
      },
      focus: function () {
        return false;
      },
      select: function (event, ui) {
        var terms = split(this.value);
        terms.pop();
        terms.push(ui.item.value);
        terms.push("");
        this.value = terms.join("; ");
        return false;
      }
    });

  $("input#Tags")
    .bind("keydown", function (event) {
      if (event.keyCode === $.ui.keyCode.TAB && $(this).data("ui-autocomplete").menu.active) {
        event.preventDefault();
      }
    })
    .autocomplete({
      source: function (request, response) {
        $.getJSON(SITE_ROOT + "Article/GetUniqueTags", {
          query: extractLast(request.term)
        }, response);
      },
      search: function () {
        var term = extractLast(this.value);
        if (term.length < 2) {
          return false;
        }
      },
      focus: function () {
        return false;
      },
      select: function (event, ui) {
        var terms = split(this.value);
        terms.pop();
        terms.push(ui.item.value);
        terms.push("");
        this.value = terms.join("; ");
        return false;
      }
    });
});