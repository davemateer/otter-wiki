$(document).ready(function () {
  $('input[type=radio][data-otter-permission=true]').on('change', function () {
    var specified = $(this).val() === "Specified";
    $(this).closest('.form-group').find('input[type=text][data-otter-permission-content=users]').prop('disabled', !specified);
  });

  function split(val) {
    return val.split(/;\s*/);
  }

  function extractLast(term) {
    return split(term).pop();
  }

  $('input[type=text][data-otter-permission=true][data-otter-permission-content=users]')
    .bind("keydown", function (event) {
      console.log('keydown');
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

$.validator.setDefaults({
  highlight: function (element, errorClass, validClass) {
    if (element.type === 'radio') {
      this.findByName(element.name).addClass(errorClass).removeClass(validClass);
    } else {
      $(element).addClass(errorClass).removeClass(validClass);
      $(element).closest('.form-group').removeClass('has-success').addClass('has-error');
    }
  },
  unhighlight: function (element, errorClass, validClass) {
    if (element.type === 'radio') {
      this.findByName(element.name).removeClass(errorClass).addClass(validClass);
    } else {
      $(element).removeClass(errorClass).addClass(validClass);
      $(element).closest('.form-group').removeClass('has-error').addClass('has-success');
    }
  }
});

$(document).ready(function () {
  $("span.field-validation-valid, span.field-validation-error").addClass('help-block');
  $("div.form-group").has("span.field-validation-error").addClass('has-error');
  $("div.validation-summary-errors").has("li:visible").addClass("alert alert-block alert-danger");
});