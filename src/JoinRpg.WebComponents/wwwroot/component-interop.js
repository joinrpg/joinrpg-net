function cachedScript(url, options) {

  // Allow user to set any option except for dataType, cache, and url
  options = $.extend(options || {}, {
    dataType: "script",
    cache: true,
    url: url
  });

  // Use $.ajax() since it is more flexible than $.getScript
  // Return the jqXHR object so we can chain callbacks
  return jQuery.ajax(options);
};


export function initBootstrapSelect(ref, initialValues) {
  cachedScript("/lib/bootstrap-select/js/bootstrap-select.js", {
    complete: function (result) {
      $(ref).selectpicker();
      $(ref).selectpicker('val', initialValues);
      // TODO why we need to force set?
    }
  });
};

export function refreshBootstrapSelect(ref) {
  $(ref).selectpicker('refresh');
};

export function getSelectedValues(ref) {
  var results = [];
  var i;
  for (i = 0; i < ref.options.length; i++) {
    if (ref.options[i].selected) {
      results[results.length] = ref.options[i].value;
    }
  }
  return results;
}

export function showModal(dialog) {
  try {
    dialog.showModal();
  } catch (e) {
    console.error('Ошибка при открытии:', e);
  }
}

export function closeModal(dialog) {
  dialog.close();
}
