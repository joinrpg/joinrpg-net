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


let bootstrapSelectLoading = null;

// Один промис загрузки плагина на модуль: init и refresh ждут одного и того же,
// иначе refresh, пришедший до загрузки скрипта, падает с «selectpicker is not a function».
function loadBootstrapSelect() {
  if (!bootstrapSelectLoading) {
    // Плагин берём из статики самой библиотеки, а не из /lib хоста: у каталога компонентов своей копии нет.
    // Сбой загрузки не бросаем: исключение ушло бы в OnAfterRenderAsync и уронило остров, а обычный <select> работает и без плагина.
    bootstrapSelectLoading = new Promise(function (resolve) {
      cachedScript("/_content/JoinRpg.Common.WebComponents/lib/bootstrap-select/js/bootstrap-select.js")
        .done(function () { resolve(true); })
        .fail(function (jqXHR, textStatus, error) {
          bootstrapSelectLoading = null; // Дать следующему вызову попробовать снова.
          console.error("Не удалось загрузить bootstrap-select:", textStatus, error);
          resolve(false);
        });
    });
  }
  return bootstrapSelectLoading;
}

export async function initBootstrapSelect(ref, initialValues) {
  if (!await loadBootstrapSelect()) {
    return;
  }
  $(ref).selectpicker();
  $(ref).selectpicker('val', initialValues);
  // TODO why we need to force set?
};

export async function refreshBootstrapSelect(ref) {
  if (!await loadBootstrapSelect()) {
    return;
  }
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
