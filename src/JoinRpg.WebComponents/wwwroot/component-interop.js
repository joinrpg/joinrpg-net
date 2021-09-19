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
  cachedScript("/_content/JoinRpg.WebComponents/lib/bootstrap-select/js/bootstrap-select.js", {
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

