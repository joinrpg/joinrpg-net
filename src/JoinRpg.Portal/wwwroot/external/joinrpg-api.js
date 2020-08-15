function JoinRpgRoles(elem, settings, $) {
  this.$el = elem;
  this.$ = $;
  this._Settings = settings;
  this.loadRoles(elem.attr('data-projectId'), elem.attr('data-locationId'));
}

JoinRpgRoles.prototype.loadRoles = function (projectId, locationId) {
  var url = this._Settings.baseUrl  || 'https://joinrpg.ru/'; 
  url = url[url.length - 1] != '/' ? url + '/' : url;
  this.$.ajax({
           url: url + projectId + '/roles/' + locationId + '/indexjson',
           dataType: 'json',
           type: 'GET',
           success: function (data) {
             this.drawRoles(this.parseData(data));
           }.bind(this),
           error: function () {
             alert('Не удалось загрузить ролевку');
           }
         })
};

JoinRpgRoles.prototype.parseData = function (data) {
  var res = [];
  var groupHash = {};
  for (var i = 0; i < data.Groups.length; i++) {
    var group = data.Groups[i],
        parentId = group.PathIds.length > 0 ? group.PathIds[group.DeepLevel - 1] : undefined,
        parent = parentId ? groupHash[parentId] : null;
    if (parent === null) {
      res.push(group);
    } else if (parent !== undefined) {
      parent.childGroups = parent.childGroups || [];
      parent.childGroups.push(group);
    }
    groupHash[group.CharacterGroupId] = group;
  }
  return res;
};

JoinRpgRoles.prototype.drawRoles = function (roles) {
  var table = this.$('<table class="joinrpg-roles-table"><tbody></tbody></table>');
  this.$el.empty();
  this.$el.append(table);
  for (var i = 0; i < roles.length; i++) {
    this.appendGroup(table.find('tbody'), roles[i]);
  }
};

JoinRpgRoles.prototype.appendGroup = function (block, group) {
  var headerTemplate = this.$('<tr><td colspan="2" class="joinrpg-roles-location-header"></td><td class="joinrpg-roles-directclaim"></td></tr>'),
      descrTemplate = this.$('<tr><td colspan="3" class="joinrpg-roles-location-description"></td></tr>'),
      header = headerTemplate.clone(),
      descr;
    header.find('td.joinrpg-roles-location-header').html(group.Path.join(this._Settings.dash) + this._Settings.dash + group.Name);
    if (group.CanAddDirectClaim) {
      var text = this.getVacanciesText(group.DirectClaimsCount)+'<br/>';
      if (group.DirectClaimsCount != 0) {
        header.find('td.joinrpg-roles-directclaim').html('<a href="' + group.DirectClaimLink + '">' + text + 'Заявиться в регион</a>');
      }
    }
    block.append(header);
    if (group.Description) {
      descr = descrTemplate.clone();
      descr.find('td.joinrpg-roles-location-description').html(group.Description);
      block.append(descr);
    }
    this.appendCharacters(block, group.Characters);
  if (group.childGroups) {
    for (var i = 0; i < group.childGroups.length; i++) {
      this.appendGroup(block, group.childGroups[i]);
    }
  }
};

JoinRpgRoles.prototype.appendCharacters = function (block, characters) {
  var charTemplate = this.$('<tr><td class="joinrpg-role"></td><td class="joinrpg-description"></td><td class="joinrpg-application"></td></tr>');
  for (var i = 0; i < characters.length; i++) {
    var char = charTemplate.clone(),
        character = characters[i];
    char.find('.joinrpg-role').html('<a href="' + character.CharacterLink + '">' + character.CharacterName + '</a> ')
        .end().find('.joinrpg-description').html(character.Description)
        .end().find('.joinrpg-application')
        .html(character.PlayerLink != null ?
                  '<a href="' + character.PlayerLink + '">' + character.PlayerName + '</a> '
                  : ' ' +
            (character.ActiveClaimsCount > 0 ? '<span class="already-claimed">Подано заявок: ' + character.ActiveClaimsCount + '</span><br>' : '') + 
                       (!!character.ClaimLink ? ' <a href="' + character.ClaimLink + '" class="joinrpg-claim-btn">Заявиться</a>' : ''));
    block.append(char);
  }
};

JoinRpgRoles.prototype.getVacanciesText = function (number) {
  var abs = number % 10;
  if (number == -1) {
    return 'Есть вакансии. ';
  }
  else if (number == 0) {
    return 'Нет вакансий';
  }
  else if ([0, 5, 6, 7, 8, 9].indexOf(abs) >= 0 || (number >= 11 && number <= 19)) {
    return number + ' вакансий. ';
  }
  else if (abs == 1) {
    return number + ' вакансия. ';
  }
  else if ([2, 3, 4].indexOf(abs) >= 0) {
    return number + ' вакансии. ';
  }
  return '';
};

(function ($) {
  $.fn.joinrpgroles = function (method) {
    var methods = {
      'destroy': function () {
        this.destroy();
        $.removeData(this.$el, 'control');
      },

      'create': function (options) {
        var settings = $.extend({
                                  dash: ' → '
                                }, options);

        this.each(function () {
          var control = new JoinRpgRoles($(this), settings, $);
          $(this).data('control', control);
        });
      }
    };

    if (methods[method]) {
      var control = this.data('control');
      return methods[method].apply(control, Array.prototype.slice.call(arguments, 1));
    } else if (typeof method === 'object' || !method) {
      return methods.create.apply(this, arguments);
    } else {
      $.error('Метод с именем ' + method + ' не существует для jQuery.JoinRpgRoles');
    }

    return this;
  };
}(jQuery));
