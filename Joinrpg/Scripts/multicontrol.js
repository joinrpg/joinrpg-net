/**
 * Copyright (C) 2012 Paul Thurlow
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

(function() {

  /**
   * @decription Trie class for saving data by keywords accessible through
   *   word prefixes
   * @class
   * @version 0.1.5
   */
  var Triejs = function(opts) {

    /**
     * @private
     * @description Options for trie implementation
     * @type {Object}
     */
    this.options = {

      /**
       * @description Maximum number of items to cache per node
       * @type {Number}
       */
      maxCache: 10

      /**
       * @description Whether to handle caching on node levels
       * @type {Boolean}
       */
      , enableCache: true

      /**
       * @description Maintain insert ordering when adding to non cached trie
       * @type {Boolean}
       */
      , insertOrder: false

      /**
       * @description Return responses from root when requests is empty
       * @type {Boolean}
       */
      , returnRoot: false

      /**
       * @description Insert function for adding new items to cache
       * @type {Function}
       */
      , insert: null

      /**
       * @description Sorting function for sorting items to cache
       * @type {Function}
       */
      , sort: null

      /**
       * @description Clip function for removing old items from cache
       * @type {Function}
       */
      , clip: null

      /**
       * @description copy function for copying data between nodes
       * @type {Function}
       */
      , copy: null

      /**
       * @description Merge function to merge two data sets together
       * @type {Function}
       */
      , merge: null
    };

    /**
     * @private
     * @description trie object
     * @type {Object}
     */
    this.root = {};

    /**
     * @private
     * @description insert order index
     * @type {Number}
     */
    this.index = 0;

    // mixin optional override options
    for (var key in opts) {
      if (opts.hasOwnProperty(key)) {
        this.options[key] = opts[key];
      }
    };

    if (typeof this.options.insert != 'function') {
      this.options.insert = function(target, data) {
        // if maintaining insert ordering add a order index on insert
        if (this.options.insertOrder
            && typeof data.d === 'undefined'
            && typeof data.o === 'undefined') {
          data = { d: data, o: this.index++ };
        }
        if (target && target.length) {
          target.push(data);
        } else {
          target = [data];
        }
        return target;
      };
    }
    if (typeof this.options.sort != 'function') {
      if (!this.options.insertOrder) {
        this.options.sort = function() {
          this.sort();
        };
      } else if (this.options.insertOrder) {
        this.options.sort = function() {
          this.sort(function(a, b) { return a.o - b.o; });
        }
      }
    }
    if (typeof this.options.clip != 'function') {
      this.options.clip = function(max) {
        if (this.length > max) {
          this.splice(max, this.length - max);
        }
      };
    }
    if (typeof this.options.copy != 'function') {
      this.options.copy = function(data) {
        return data.slice(0);
      }
    }
    if (typeof this.options.merge != 'function') {
      this.options.merge = function(target, data, word) {
        for (var i = 0, ii = data.length; i < ii; i++) {
          target = this.options.insert.call(this, target, data[i]);
          this.options.sort.call(target, word);
          this.options.clip.call(target, this.options.maxCache);
        }
        return target;
      }
    }
  };

  Triejs.prototype = {

    /*-------------------------------------------------------------------------
     * Private Functions
     -------------------------------------------------------------------------*/

    /**
     * @description Add data to the current nodes cache
     * @param curr {Object} current node in trie
     * @param data {Object} Data to add to the cache
     * @private
     */
    _addCacheData: function(curr, data) {
      if ((this.root === curr && !this.options.returnRoot)
          || this.options.enableCache === false) {
        return false;
      }
      if (!curr.$d) {
        curr.$d = {};
      }
      curr.$d = this.options.insert.call(this, curr.$d, data);
      this.options.sort.call(curr.$d);
      this.options.clip.call(curr.$d, this.options.maxCache);
      return true;
    }

    /**
     * @description Adds the remainder of a word to a subtree
     * @param suffix {String} the remainder of a word
     * @param data {Object} data to add at suffix
     * @param curr {Object} current node in the trie
     * @private
     */
    , _addSuffix: function(suffix, data, curr) {
      var letter = suffix.charAt(0)
          , nextSuffix = suffix.substring(1) || null
          , opts = { $d: {} };
      if (nextSuffix) {
        opts.$s = nextSuffix;
      }
      if (typeof curr[letter] === 'undefined') {
        curr[letter] = opts;
      } else if (typeof curr[letter].$d === 'undefined') {
        curr[letter].$d = {};
        if (nextSuffix && typeof curr[letter].$s === 'undefined') {
          curr[letter].$s = nextSuffix;
        }
      }
      curr[letter].$d = this.options.insert.call(this, curr[letter].$d, data);
      this.options.sort.call(curr[letter].$d);
    }

    /**
     * @description Move data from current location to new suffix position
     * @param suffix {String} the remainder of a word
     * @param data {Object} data currently stored to be moved to suffix ending
     * @param curr {Object} current node in the trie
     * @private
     */
    , _moveSuffix: function(suffix, data, curr) {
      var letter = suffix.charAt(0)
          , nextSuffix = suffix.substring(1) || null
          , opts = { $d: {} };
      if (nextSuffix) {
        opts.$s = nextSuffix;
      }
      if (typeof curr[letter] === 'undefined') {
        curr[letter] = opts;
      }
      curr[letter].$d = this.options.copy(data);
    }

    /**
     * @description Get data from a given node, either in the cache
     *   or by parsing the subtree
     * @param node {Object} The node to get data from
     * @return {Array|Object} data results
     */
    , _getDataAtNode: function(node, word) {
      var data;

      if (this.options.enableCache) {
        this.options.sort.call(node.$d, word);
        data = node.$d;
      } else {
        data = this._getSubtree(node, word);
      }
      if (this.options.insertOrder) {
        data = this._stripInsertOrder(data);
      }
      return data ? this.options.copy(data) : undefined;
    }

    /**
     * @description Remove the outer data later that stores insert order
     * @param data {Object} The data with insert order object wrapper
     * @return {Array} data results without insert order wrapper
     */
    , _stripInsertOrder: function(data) {
      if (typeof data == 'undefined') {
        return;
      }
      var temp = [];
      for (var i = 0, ii = data.length; i < ii; i++) {
        temp.push(data[i].d);
      }
      return temp;
    }

    /**
     * @description Get the subtree data of a trie traversing depth first
     * @param curr {Object} current node in the trie to get data under
     * @return {Object} data from the subtree
     */
    , _getSubtree: function(curr, word) {
      var res
          , nodeArray = [curr]
          , node;
      while (node = nodeArray.pop()) {
        for (var newNode in node) {
          if (node.hasOwnProperty(newNode)) {
            if (newNode == '$d') {
              if (typeof res == 'undefined') {
                res = [];
              }
              res = this.options.merge.call(this, res, node.$d, word);
            } else if (newNode != '$s') {
              nodeArray.push(node[newNode]);
            }
          }
        }
      }
      return res;
    }

    /*-------------------------------------------------------------------------
     * Public Functions
     -------------------------------------------------------------------------*/

    /**
     * @description Adds a word into the trie
     * @param word {String} word to add
     * @param data {Object} data to store under given term
     */
    , add: function(word, data) {
      if (typeof word != 'string') { return false; }
      if (arguments.length == 1) { data = word; }
      word = word.toLowerCase();

      var curr = this.root;

      for (var i = 0, ii = word.length; i < ii; i++) {
        var letter = word.charAt(i);
        // No letter at this level
        if (!curr[letter]) {
          // Current level has a suffix already so push suffix lower in trie
          if (curr.$s) {
            if (curr.$s == word.substring(i)) {
              // special case where word exists already, so we avoid breaking
              // up the substring and store both at the top level
              if (!this._addCacheData(curr, data)) {
                curr.$d = this.options.insert.call(this, curr.$d, data);
                this.options.sort.call(curr.$d);
              }
              break;
            }
            this._moveSuffix(curr.$s, curr.$d, curr);
            delete curr.$s;
            if (this.options.enableCache === false) {
              delete curr.$d;
            }
          }
          // Current level has no sub letter after building suffix
          if (!curr[letter]) {
            this._addSuffix(word.substring(i), data, curr);
            this._addCacheData(curr, data);
            break;
          }
          // add to the cache at the current node level in the trie
          this._addCacheData(curr, data);
          // if its the end of a word push possible suffixes at this node down
          // and add data to cache at the words end
          if (i == ii - 1) {
            if (curr[letter].$s) {
              this._moveSuffix(curr[letter].$s, curr[letter].$d, curr[letter]);
              delete curr[letter].$s;
              if (this.options.enableCache === false) {
                delete curr[letter].$d;
              }
              // insert new data at current end of word node level
              this._addSuffix(letter, data, curr);
            } else {
              // either add to cache or just add the data at end of word node
              if (!this._addCacheData(curr[letter], data)) {
                this._addSuffix(letter, data, curr);
              }
            }
          }
          curr = curr[letter];
        }
        // There is a letter and we are at the end of the word
        else if (i == ii - 1) {
          this._addCacheData(curr, data);
          // either add to cache at the end of the word or just add the data
          if (!this._addCacheData(curr[letter], data)) {
            this._addSuffix(letter, data, curr);
          }
        }
        // There is a letter so traverse lower into the trie
        else {
          this._addCacheData(curr, data);
          curr = curr[letter];
        }
      }
    }

    /**
     * @description remove a word from the trie if there is no caching
     * @param word {String} word to remove from the trie
     */
    , remove: function(word) {
      if (typeof word !== 'string' || word === '' || this.options.enableCache){
        return;
      }
      word = word.toLowerCase();
      var letter
          , i
          , ii
          , curr = this.root
          , prev
          , prevLetter
          , data
          , count = 0;

      for (i = 0, ii = word.length; i < ii; i++) {
        letter = word.charAt(i);
        if (!curr[letter]) {
          if (curr.$s && curr.$s === word.substring(i)) {
            break; // word is at this leaf node
          } else {
            return; // word not in the trie
          }
        } else {
          prev = curr;
          prevLetter = letter;
          curr = curr[letter]
        }
      }
      data = this.options.copy(curr.$d);
      if (this.options.insertOrder) {
        data = this._stripInsertOrder(data);
      }
      delete curr.$d;
      delete curr.$s;
      // enumerate all child nodes
      for (var node in curr) {
        if (curr.hasOwnProperty(node)) {
          count++;
        }
      }
      if (!count) {
        delete prev[prevLetter]; // nothing left at this level so remove it
      }
      return data;
    }

    /**
     * @description see if a word has been added to the trie
     * @param word {String} word to search for
     * @return {Boolean} whether word exists or not
     */
    , contains: function(word) {
      if (typeof word !== 'string' || word == '') { return false; }
      word = word.toLowerCase();

      var curr = this.root;
      for (var i = 0, ii = word.length; i < ii; i++) {
        var letter = word.charAt(i);
        if (!curr[letter]) {
          if (curr.$s && curr.$s === word.substring(i)) {
            return true;
          } else {
            return false;
          }
        } else {
          curr = curr[letter];
        }
      }
      return curr.$d && (typeof curr.$s === 'undefined') ? true : false;
    }

    /**
     * @description Get the data for a given prefix of a word
     * @param prefix {String} string of the prefix of a word
     * @return {Object} data for the given prefix
     */
    , find: function(prefix) {
      if (typeof prefix !== 'string') { return undefined; }
      if (prefix == '' && !this.options.returnRoot) { return undefined; }
      prefix = prefix.toLowerCase();

      var curr = this.root;
      for (var i = 0, ii = prefix.length; i < ii; i++) {
        var letter = prefix.charAt(i);
        if (!curr[letter]) {
          if (curr.$s && curr.$s.indexOf(prefix.substring(i)) == 0) {
            return this._getDataAtNode(curr, prefix);
          } else {
            return undefined;
          }
        } else {
          curr = curr[letter];
        }
      }
      return this._getDataAtNode(curr, prefix);
    }
  };

  //Export to CommonJS/Node format
  if (typeof exports !== 'undefined') {
    if (typeof module !== 'undefined' && module.exports) {
      exports = module.exports = Triejs;
    }
    exports.Triejs = Triejs;
  } else if (typeof define === 'function' && define.amd) {
    define('triejs', function() {
      return Triejs;
    });
  } else {
    // no exports so attach to global
    this['Triejs'] = Triejs;
  }
})(this);
var MultiControl = MultiControl || {};

function Action(id, actionManager) {
  this.ActionManager = actionManager;
  this.Id = (typeof id == 'string' ? id : id.id);
}

Action.prototype.trigger = function () {
  this.ActionManager.trigger(true, this.Id, arguments);
};

function Request(id, actionManager) {
  this.ActionManager = actionManager;
  this.Id = (typeof id == 'string' ? id : id.id);
}

Request.prototype.trigger = function () {
  return this.ActionManager.trigger(false, this.Id, arguments);
};


MultiControl.ActionManager = function () {
  this.Listeners = {};
  this.Requests = {};
};

MultiControl.ActionManager.prototype.createAction = function (create_params) {
  return new Action(create_params, this);
};

MultiControl.ActionManager.prototype.createRequest = function (create_params) {
  return new Request(create_params, this);
};


MultiControl.ActionManager.prototype.registerListener = function (action, listener, handler) {
  if (!this.Listeners[action]) {
    this.Listeners[action] = [];
  }
  for (var i = 0; i < this.Listeners[action].length; i++) {
    if (this.Listeners[action][i].listener === listener) {
      return false;
    }
  }
  this.Listeners[action].push({listener: listener, handler: handler});
};

MultiControl.ActionManager.prototype.unregisterListener = function (action, listener) {
  if (!!this.Listeners[action]) {
    for (var i = 0; i < this.Listeners[action].length; i++) {
      if (this.Listeners[action][i].listener === listener) {
        this.Listeners[action].splice(i, 1);
        return true;
      }
    }
  }
  return false;
};

MultiControl.ActionManager.prototype.registerRequest = function (action, listener, handler) {
  this.Requests[action] = {listener: listener, handler: handler};
};

MultiControl.ActionManager.prototype.trigger = function (isAction, action, params) {
  if (isAction) {
    if (!!this.Listeners[action]) {
      this.Listeners[action].forEach(function (listener) {
        listener.listener[listener.handler].apply(listener.listener, params);
      })
    }
  } else {
    if (!!this.Requests[action]) {
      var listener = this.Requests[action];
      return listener.listener[listener.handler].apply(listener.listener, params);
    }
  }
};

MultiControl.ActionManager.prototype.exists = function (event) {
  return (!!this.Listeners[event] || !!this.Requests[event]);
};

MultiControl.ActionManager.prototype.destroy = function () {
  delete this.Listeners;
  delete this.Requests;
};
var MultiControl = MultiControl || {};
MultiControl.Models = MultiControl.Models || {};

//вопрос терминологи открыт
//какие заявки допустимы:
//любые, любые не более лимита, только на прописанных персонажей
MultiControl.Models.InGroupApplications = {
  AnyApplications: 0,
  LimitedApplications: 1,
  OnlyCharactersApplications: 2
};

MultiControl.Models.Group = function (options, actionManager, checkcircles) {
  var action_manager = actionManager,
      check_circles = checkcircles;
  var id = options.id;
  this.getId = function () {
    return id;
  };
  var name = options.name;
  this.getName = function () {
    return name;
  };
  this.setName = function (nm) {
    name = nm;
  };
  var checkable = options.checkable;
  this.isCheckable = function () {
    return checkable;
  };
  var checked = options.checked;
  this.isChecked = function () {
    return checked;
  };
  this.setChecked = function (c) {
    checked = c;
  };
  var selected = options.selected;
  this.isSelected = function () {
    return selected;
  };
  this.setSelected = function (s, quiet) {
    //если checkcircles == children
    //то проверяем, что s не является родителем keyelements
    var circle_check = false;
    if (check_circles != 'none'){
      var key_elements = action_manager.createRequest('get_key_items').trigger();
      var queue = [],
          curpos = 0;
      for (var i = 0; i < key_elements.length; i++) {
        queue.push(key_elements[i]);
      }
    }
    if (check_circles == 'parents') {
      while (curpos < queue.length) {
        if (queue[curpos] === this) {
          circle_check = true;
          break;
        }
        if (queue[curpos].getType() == 'group') {
          var children = queue[curpos].getChildGroups();
          queue = queue.concat(children);
        }
        curpos++;
      }
    }
    else if (check_circles == 'children'){
//      while (curpos < queue.length) {
//        if (queue[curpos] === this) {
//          circle_check = true;
//          break;
//        }
//        if (queue[curpos].getType() == 'group') {
//          var parents = queue[curpos].getParents();
//          queue = queue.concat(parents);
//        }
//        curpos++;
//      }
    }

    if (circle_check) {
      alert('Вы попытались сделать родителем группы ее потомка. Это создаст парадокс. Мы не любим парадоксы');
      return false;
    }
    else {
      selected = s;
      if (!quiet) {
        action_manager.createAction('group_selected_' + id).trigger(s);
        if (s) {
          action_manager.createAction('item_selected').trigger(this);
        }
        else {
          action_manager.createAction('item_unselected').trigger(this);
        }
      }
      return true;
    }
  };

  var allowedApplications = options.all_apps;
  this.getAllowedApplications = function () {
    return allowedApplications;
  };
  this.setAllowedApplications = function (type) {
    allowedApplications = type;
  };

  var parentGroups = [];
  this.getParents = function () {
    return parentGroups;
  };
  this.addParent = function (parent, place) {
    place = (typeof place == 'undefined' ? parentGroups.length : place);
    parentGroups.splice(place, 0, parent);
    parent.addChildGroup(this);
  };
  this.removeParent = function (parent) {
    for (var i = parentGroups.length - 1; i >= 0; --i) {
      if (parentGroups[i].getId() == parent.getId()) {
        parentGroups.splice(i, 1);
        return;
      }
    }
  };

  var childGroups = [];
  this.getChildGroups = function () {
    return childGroups;
  };
  this.addChildGroup = function (child, place) {
    place = (typeof place == 'undefined' ? childGroups.length : place);
    childGroups.splice(place, 0, child);
  };
  this.removeChildGroup = function (child) {
    for (var i = childGroups.length - 1; i >= 0; --i) {
      if (childGroups[i] / getId() == child.getId()) {
        childGroups.splice(i, 1);
        return;
      }
    }
  };

  var childCharacters = [];
  this.getChildCharacters = function () {
    return childCharacters;
  };
  this.addChildCharacter = function (child, place) {
    place = (typeof place == 'undefined' ? childCharacters.length : place);
    childCharacters.splice(place, 0, child);
  };
  this.removeChildCharacter = function (child) {
    for (var i = childCharacters.length - 1; i >= 0; --i) {
      if (childCharacters[i].getId() == child.getId()) {
        childCharacters.splice(i, 1);
        return;
      }
    }
  };

  this.getType = function () {
    return 'group';
  };

  this.clear = function () {
    parentGroups = [];
    delete parentGroups;
    childCharacters = [];
    delete childCharacters;
    childGroups = [];
    delete childGroups;
    allowedApplications = undefined;
    delete allowedApplications;
  };
};

MultiControl.Models.Character = function (options, actionManager, checkcircles) {
  var action_manager = actionManager,
      check_circles = checkcircles;
  var id = options.id;
  this.getId = function () {
    return id;
  };
  var name = options.name;
  this.getName = function () {
    return name;
  };
  this.setName = function (nm) {
    name = nm;
  };
  var checkable = options.checkable;
  this.isCheckable = function () {
    return checkable;
  };
  var checked = options.checked;
  this.isChecked = function () {
    return checked;
  };
  this.setChecked = function (c) {
    checked = c;
  };
  var selected = options.selected;
  this.isSelected = function () {
    return selected;
  };
  this.setSelected = function (s, quiet) {
    if (check_circles == 'parents'){
      alert('Персонаж не может быть родителем');
      return false;
    }
    selected = s;
    if (!quiet) {
      action_manager.createAction('character_selected_' + id).trigger(s);
      if (s) {
        action_manager.createAction('item_selected').trigger(this);
      } else {
        action_manager.createAction('item_unselected').trigger(this);
      }
    }
    return true;
  };

  var parentGroups = [];
  this.getParents = function () {
    return parentGroups;
  };
  this.addParent = function (parent, place) {
    place = (typeof place == 'undefined' ? parentGroups.length : place);
    parentGroups.splice(place, 0, parent);
    parent.addChildCharacter(this);
  };
  this.removeParent = function (parent) {
    for (var i = parentGroups.length - 1; i >= 0; --i) {
      if (parentGroups[i].getId() == parent.getId()) {
        parentGroups.splice(i, 1);
        return;
      }
    }
  };

  this.getType = function () {
    return 'character';
  };


  this.clear = function () {
    parentGroups = [];
    delete parentGroups;
  }
};

MultiControl.Models.InputModel = function (isMultiselect, actionManager) {
  var selectedItems = [],
      is_multiselect = isMultiselect,
      action_manager = actionManager;
  this.addItem = function (item, place) {
    if (is_multiselect) {
      place = (typeof place == 'undefined' ? selectedItems.length : place);
      selectedItems.splice(place, 0, item);
    }
    else {
      selectedItems = [item];
    }
    action_manager.createAction('update_input').trigger();
  };
  this.removeItem = function (item) {
    for (var i = selectedItems.length - 1; i >= 0; --i) {
      if (selectedItems[i].getId() == item.getId()) {
        selectedItems.splice(i, 1);
        action_manager.createAction('update_input').trigger();
        return;
      }
    }
  };
  this.getSelectedItems = function () {
    return selectedItems;
  };

  action_manager.registerListener('item_selected', this, 'addItem');
  action_manager.registerListener('item_unselected', this, 'removeItem');
  action_manager.registerRequest('get_selected_items', this, 'getSelectedItems')
};

MultiControl.Models.AllGroupsModel = function (actionManager, implicitgroups) {
  var all_groups = [],
      action_manager = actionManager,
      implicit_groups = implicitgroups;

  this.updateGroups = function () {
    if (implicit_groups == 'parents') {
      this.doUpdateParents();
    }
    else if (implicit_groups == 'children') {
      this.doUpdateChildren();
    }
    else if (implicit_groups == 'none') {
      all_groups = [];
    }
  };

  this.doUpdateParents = function () {
    function appendParents(parents) {
      for (var j = 0; j < parents.length; j++) {
        var id = parents[j].getId();
        if (id != root_id && all_groups_ids.indexOf(id) < 0
            && selected_items_ids.indexOf(id) < 0) {
          all_groups_ids.push(parents[j].getId());
        }
      }
    }

    var all_groups_ids = [];
    var selected_items = action_manager.createRequest('get_selected_items').trigger();
    var root_id = action_manager.createRequest('get_root_model_id').trigger();
    var selected_items_ids = [];
    var curpos = 0;
    var queue = [];

    var i, j, parents;
    for (i = 0; i < selected_items.length; i++) {
      var cur_item = selected_items[i];
      selected_items_ids.push(cur_item.getId());
      parents = cur_item.getParents();
      appendParents(parents);
      queue = queue.concat(parents);
    }
    while (curpos < queue.length) {
      parents = queue[curpos].getParents();
      appendParents(parents);
      queue = queue.concat(parents);
      curpos++;
    }

    all_groups = [];
    for (i = 0; i < all_groups_ids.length; i++) {
      all_groups.push(action_manager.createRequest('get_model_by_id').trigger(all_groups_ids[i]));
    }
  };

  this.doUpdateChildren = function () {
    function appendChildCharacters(children) {
      for (var j = 0; j < children.length; j++) {
        var id = children[j].getId();
        if (all_groups_ids.indexOf(id) < 0
            && selected_items_ids.indexOf(id) < 0) {
          all_groups_ids.push(children[j].getId());
        }
      }
    }

    var all_groups_ids = [];
    var selected_items = action_manager.createRequest('get_key_items').trigger();
    var selected_items_ids = [];
    var curpos = 0;
    var queue = [];

    var i, j, child_groups, child_characters;
    for (i = 0; i < selected_items.length; i++) {
      var cur_item = selected_items[i];
      selected_items_ids.push(cur_item.getId());
      if (cur_item.getType() == 'group') {
        child_characters = cur_item.getChildCharacters();
        appendChildCharacters(child_characters);
        child_groups = cur_item.getChildGroups();
        queue = queue.concat(child_groups);
      }
    }

    while (curpos < queue.length) {
      if (cur_item.getType() == 'group') {
        child_characters = queue[curpos].getChildCharacters();
        appendChildCharacters(child_characters);
        child_groups = queue[curpos].getChildGroups();
        queue = queue.concat(child_groups);
      }
      curpos++;
    }

    all_groups = [];
    for (i = 0; i < all_groups_ids.length; i++) {
      var model = action_manager.createRequest('get_model_by_id').trigger(all_groups_ids[i]);
      console.log(model.getName());
      all_groups.push(model);
    }

  };

  this.getAllGroups = function () {
    return all_groups;
  };
};
var MultiControl = MultiControl || {};
var actionManager;

MultiControl.App = function () {
};

MultiControl.App.prototype.initializeControl = function (elem, options) {
  //setting control
  this.initializeData(elem, options);
  //adding tags
  this.applyHTML();
  //creating models and views
  this.initializeModels();
  //loading data
  this.loadData(options.url);
  //events and requests
  this.tryInitializeMultiselect();
  //initializing sending for forms
  this.tryInitializeHiddenSelect();
  //adding general listeners
  this.applyGeneralListeners();
  //applying DOM events listeners
  this.applyEvents();
};

MultiControl.App.prototype.initializeData = function (elem, options) {
  this.$el = elem;
  this.Settings = options;
  this.ActionManager = new MultiControl.ActionManager();
  this.RootGroupModel = undefined;
  this.Trie = undefined;

  this.GroupsList = {};
  this.CharacterList = {};
};

MultiControl.App.prototype.applyHTML = function () {
  this.$el.addClass('multicontrol');
  var html = '<div class="mlc-input-group"><div class="mlc-tags"><input class="mlc-taginput" type="text"></div></div>' +
      '<ul class="mlc-all-groups hidden"></ul>' +
      '<div class="mlc-multilist mlc-list hidden"><ul></ul></div>' +
      '<div class="mlc-autocomplete mlc-list hidden"><ul></ul></div>';
  this.$el.append(html);
};

MultiControl.App.prototype.initializeModels = function () {
  this.InputModel = new MultiControl.Models.InputModel(this.Settings.multiselect, this.ActionManager);
  this.InputView = new MultiControl.Views.InputView(
      this.$el.find('.mlc-input-group'), this.InputModel, this.ActionManager);
  this.MultilistView = new MultiControl.Views.MultilistView(
      this.$el.find('.mlc-multilist'), this.Settings.multiselect, this.ActionManager);
  this.AutocompleteView = new MultiControl.Views.AutocompleteView(
      this.$el.find('.mlc-autocomplete'), this.Settings.multiselect, this.ActionManager);
  this.AllGroupsView = new MultiControl.Views.AllGroupsView(
      new MultiControl.Models.AllGroupsModel(this.ActionManager, this.Settings.implicitgroups),
      this.$el.find('ul.mlc-all-groups'), this.ActionManager, this.Settings.implicitgroups);
};

MultiControl.App.prototype.loadData = function (url) {
  var sender = this;
  $.ajax({
    url: url,
    type: 'GET',
    dataType: 'json',
    success: function (data) {
      sender.processData(data.Groups);
      sender.draw();
      sender.initAutocomplete();
      sender.applyKeyElements();
    },
    error: function (data) {
      alert('Ошибка. ' + data.responseText);
    }
  });
};

MultiControl.App.prototype.tryInitializeMultiselect = function () {
  if (!this.Settings.multiselect) {
    this.ActionManager.registerRequest('get_current_selected', this, 'getCurrentSelected');
    this.ActionManager.registerListener('item_selected', this, 'currentSelected');
    this.ActionManager.registerListener('item_unselected', this, 'currentUnselected');
    var current_selected = undefined;
    this.getCurrentSelected = function () {
      return current_selected;
    };
    this.currentSelected = function (current) {
      current_selected = current;
    };
    this.currentUnselected = function () {
      current_selected = undefined;
    };
  }
};

MultiControl.App.prototype.tryInitializeHiddenSelect = function () {
  if (this.Settings.hiddenselect) {
    this.Select = $('<select class="hidden" data-val="true" id="" name="">');
    this.Select.attr('id', this.Settings.hiddenselect.id).attr('name', this.Settings.hiddenselect.name);
    if (this.Settings.multiselect) {
      this.Select.prop('multiple', true);
    }
    this.$el.after(this.Select);
    this.ActionManager.registerListener('input_updated', this, 'addValueToSelect');
  }
};

MultiControl.App.prototype.addValueToSelect = function (selected_items) {
  this.Select.empty();
  var list = [];
  for (var i = 0; i < selected_items.length; i++) {
    var id = selected_items[i].getId().split('^')[1];
    this.Select.append('<option selected="selected" value="' + id + '"></option>')
  }
};

MultiControl.App.prototype.applyGeneralListeners = function () {
  this.ActionManager.registerRequest('get_model_by_id', this, 'getModelById');
  this.ActionManager.registerRequest('get_root_model_id', this, 'getRootModelId');
  this.ActionManager.registerListener('update_input', this, 'triggerOnUpdateCallback');
};

MultiControl.App.prototype.applyEvents = function () {
  var sender = this;
  $('body:not(.multicontrol, .multicontrol *)').on('click', function (event) {
    if (!$(event.target).is('.multicontrol') && !$(event.target).is('.multicontrol *')) {
      sender.ActionManager.createAction('close_current_list').trigger();
      sender.ActionManager.createAction('show_all_groups').trigger();
      sender.$el.find('.mlc-taginput').blur();
      sender.$el.find('.mlc-input-group').removeClass('focused');
    }
  });
};

//data prcoessing
MultiControl.App.prototype.processData = function (data) {
  var i, max, group, character, parent, j,
      group_model, character_model, group_option, character_option;
  for (i = 0, max = data.length; i < max; i++) {
    group = data[i];
    if (group.FirstCopy) {
      group_option = {
        //временное решение
        id: 'group^' + group.CharacterGroupId,
        name: group.Name,
        checkable: false,
        checked: false,
        selected: false,
        app_apps: false};
      group_model = new MultiControl.Models.Group(group_option,
          this.ActionManager, this.Settings.checkcircles);
      if (group.DeepLevel == 0) {
        this.RootGroupModel = group_model;
      }
      this.GroupsList[group_model.getId()] = group_model;
    }
    else {
      group_model = this.GroupsList['group^' + group.CharacterGroupId];
    }
    if (group.PathIds.length > 0) {
      parent = this.GroupsList['group^' + group.PathIds[group.PathIds.length - 1]];
      if (!!parent) {
        group_model.addParent(parent);
      }
    }
    if (!!group.Characters) {
      for (j = 0; j < group.Characters.length; j++) {
        character = group.Characters[j];
        if (character.IsFirstCopy) {
          character_option = {
            id: 'character^' + character.CharacterId,
            name: character.CharacterName,
            checkable: false,
            checked: false,
            selected: false,
            app_apps: false
          };
          character_model = new MultiControl.Models.Character(character_option, this.ActionManager, this.Settings.checkcircles);
          this.CharacterList[character_model.getId()] = character_model;
        }
        else {
          character_model = this.CharacterList['character^' + character.CharacterId];
        }
        character_model.addParent(group_model);
      }
    }
  }
};

//creating multilist view
MultiControl.App.prototype.draw = function () {
  var root_view = this.createView(this.RootGroupModel, '');
  this.$el.find('.mlc-multilist>ul').append(root_view.getEl());
  this.$el.find('.mlc-multilist').addClass('hidden');
};

MultiControl.App.prototype.createView = function (model, parentsString) {
  var view = MultiControl.Views.ViewFactory.createView(model, parentsString,
      this.Settings.multiselect, this.ActionManager);
  this.ActionManager.createAction('add_multilist_view').trigger(view);

  if (model.getType() == 'group') {
    var child_groups = model.getChildGroups(),
        child_chars = model.getChildCharacters(),
        i;
    for (i = 0; i < child_groups.length; i++) {
      view.addGroup(this.createView(child_groups[i], view.getParentsString()));
    }
    for (i = 0; i < child_chars.length; i++) {
      if (this.Settings.showcharacters) {
        view.addCharacter(this.createView(child_chars[i], view.getParentsString()));
      }
    }
  }
  return view;
};

MultiControl.App.prototype.applyKeyElements = function () {
  this.ActionManager.registerRequest('get_key_items', this, 'getKeyItems')
  for (var i = 0; i < this.Settings.keyelements.length; i++) {
    var key_id = this.Settings.keyelements[i];
    var key_elem = this.getModelById(key_id);
    for (var j = 0, parents = key_elem.getParents(); j < parents.length; j++) {
      var parent = parents[j];
      parent.setSelected(true, false);
    }
  }
  this.ActionManager.createAction('show_all_groups').trigger();
};

//autocomplete
MultiControl.App.prototype.initAutocomplete = function () {
  this.Trie = new Triejs();
  var i, j, keys, max, words;
  for (i = 0, keys = Object.keys(this.GroupsList), max = keys.length; i < max; i++) {
    words = this.GroupsList[keys[i]].getName().split(' ');
    for (j = 0; j < words.length; j++) {
      this.Trie.add(words[j], this.GroupsList[keys[i]]);
    }
  }

  for (i = 0, keys = Object.keys(this.CharacterList), max = keys.length; i < max; i++) {
    words = this.CharacterList[keys[i]].getName().split(' ');
    for (j = 0; j < words.length; j++) {
      this.Trie.add(words[j], this.CharacterList[keys[i]]);
    }
  }

  this.ActionManager.registerRequest('get_word', this, 'getWord');
};

MultiControl.App.prototype.getWord = function (word) {
  return this.Trie.find(word);
};

//requests
MultiControl.App.prototype.getModelById = function (id) {
  var has_no_prefix = (id.indexOf('group^') < 0 && id.indexOf('character^') < 0);
  //возвращается либо группа, либо персонаж, а если нет обоих - undefined
  if (has_no_prefix) {
    var gid = 'group^' + id;
    var cid = 'character^' + id;
    return (!!this.GroupsList[gid] ? this.GroupsList[gid] : this.CharacterList[cid]);
  }
  return (!!this.GroupsList[id] ? this.GroupsList[id] : this.CharacterList[id]);
};

MultiControl.App.prototype.getKeyItems = function () {
  var list = [];
  for (var i=0; i<this.Settings.keyelements.length; i++){
    var key_id = this.Settings.keyelements[i];
    var key_elem = this.getModelById(key_id);
    list.push(key_elem);
  }
  return list;
};

MultiControl.App.prototype.getRootModelId = function () {
  return this.RootGroupModel.getId();
};

//user callbacks
MultiControl.App.prototype.triggerOnUpdateCallback = function () {
  var selected_items = this.ActionManager.createRequest('get_selected_items').trigger();
  if (!!this.Settings.onitemselected) {
    var list = [];
    for (var i = 0; i < selected_items.length; i++) {
      list.push(selected_items[i].getId());
    }
    this.Settings.onitemselected(list);
  }
  this.ActionManager.createAction('input_updated').trigger(selected_items);
};

//methods
MultiControl.App.prototype.disable = function () {
  this.$el.addClass('disabled');
  this.ActionManager.createAction('disable').trigger();
};

MultiControl.App.prototype.enable = function () {
  this.$el.removeClass('disabled');
  this.ActionManager.createAction('enable').trigger();
};

MultiControl.App.prototype.destroy = function () {
  var sender = this;
  var promise = new Promise(function (resolve, reject) {
    sender.ActionManager.createAction('destroy_elems').trigger();
    resolve();
  });
  promise.then(function () {
    sender.$el.empty();
    sender.$el.removeClass('multicontrol');
    sender.Select.detach();
    sender.ActionManager.destroy();
    delete sender.ActionManager;
    delete sender.Select;
    delete sender.MultilistView;
    delete sender.AutocompleteView;
    delete sender.AllGroupsView;
    delete sender.InputView;
    delete sender.$el;
    sender.destroyCharactersList();
    delete sender.CharacterList;
    sender.destroyGroupsList(sender.RootGroupModel);
    delete sender.GroupsList;
    delete sender.RootGroupModel;
    delete sender.Trie;
  });
};

MultiControl.App.prototype.destroyGroupsList = function (model) {
  var child_groups = model.getChildGroups(),
      i;
  for (i = 0; i < child_groups.length; i++) {
    if (child_groups[i].getChildGroups().length == 0) {
      child_groups[i].clear();
      return;
    }
    else {
      this.destroyGroupsList(child_groups[i]);
    }
  }
};

MultiControl.App.prototype.destroyCharactersList = function () {
  for (var i = 0; i < this.CharacterList.length; i++) {
    var character = this.CharacterList[i];
    character.clear();
  }
};


(function ($) {
  $.fn.multicontrol = function (method) {
    var methods = {
      'enable': function () {
        //this.ActionManager.createAction('enable_control').trigger();
        this.enable();
      },
      'disable': function () {
        //this.ActionManager.createAction('disable_control').trigger();
        this.disable();
      },
      'destroy': function () {
        this.destroy();
        $.removeData(this.$el, 'control');
      },
      'create': function (options) {
        var settings = $.extend({
          multiselect: true,
          onitemselected: undefined,
          showcharacters: true,
          hiddenselect: undefined,
          keyelements: [],
          selectedelements: 'none',
          implicitgroups: 'none',
          checkcircles: 'none'
        }, options);

        this.each(function () {
          var control = new MultiControl.App();
          control.initializeControl($(this), settings);
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
      $.error('Метод с именем ' + method + ' не существует для jQuery.tooltip');
    }

    return this;
  };
}(jQuery));

(function ($) {
  $.fn.getCursorPosition = function () {
    var input = this.get(0);
    if (!input) return; // No (input) element found
    if ('selectionStart' in input) {
      // Standard-compliant browsers
      return input.selectionStart;
    } else if (document.selection) {
      // IE
      input.focus();
      var sel = document.selection.createRange();
      var selLen = document.selection.createRange().text.length;
      sel.moveStart('character', -input.value.length);
      return sel.text.length - selLen;
    }
  }
})(jQuery);

//TODO: имплицитные группы вверх-вниз
//TODO: checkcircles
//TODO: selectedelements
//TODO: долгое нажатие на стрелочку
//TODO: выписывать путь в теге (без "все роли!") - из разных групп!
//TODO: настройка, показывать ли рут
//TODO: настройка приема данных
//TODO: крутилка на загрузке
;
$(function () {
  var options = {
    url: 'http://dev.joinrpg.ru/27/roles/78/indexjson',
    multiselect: true,
    onitemselected: function (items) {
    },
    showcharacters: true,
    hiddenselect: {
      id: 'testselect',
      name: 'testselect'
    },
    keyelements: ['2279'],
    selectedelements: 'none',
    implicitgroups: 'children',
    checkcircles: 'children'
  };
  var c = $('#jrpg-multicontrol');
  c.multicontrol(options);
});
var MultiControl = MultiControl || {};
MultiControl.Views = MultiControl.Views || {};

MultiControl.Views.ViewFactory = MultiControl.Views.ViewFactory || {};
MultiControl.Views.ViewFactory.createView = function (model, parentsString, isMultiselect, actionManager) {
  var type = model.getType();
  switch (type) {
    case 'group':
      return new MultiControl.Views.GroupView(model, parentsString,
          isMultiselect, actionManager);
    case 'character':
      return new MultiControl.Views.CharacterView(model, parentsString,
          isMultiselect, actionManager);
    default:
      return undefined;
  }
};

MultiControl.Views.GroupView = function (model, parentsString, isMultiselect, actionManager) {
  var group = model,
      $el = $('<li class="group"><span class="title">' +
          '<span class="mlc-collapser lsf opened" title="Свернуть группу">up</span></span>' +
          '<div class="ingroup">' +
          '<ul class="childgroups"></ul><ul class="childchars"></ul></div></li>'),
      parents = (parentsString == '' ? model.getName() : parentsString + ' - ' + model.getName()),
      is_multiselect = isMultiselect,
      action_manager = actionManager,
      disabled = false;
  $el.attr('group-id', model.getId());
  $el.find('.title').html(group.getName() + $el.find('.title').html());
  $el.find('.title').attr('title', parents);
  //methods
  this.getEl = function () {
    return $el;
  };
  this.getParentsString = function () {
    return parents;
  };
  this.getModel = function () {
    return group;
  };

  this.addCharacter = function (c) {
    $el.children('.ingroup').children('.childchars').append(c.getEl())
  };
  this.addGroup = function (g) {
    $el.children('.ingroup').children('.childgroups').append(g.getEl());
  };

  //listeners
  action_manager.registerListener('group_selected_' + group.getId(), this, 'onModelSelected');
  action_manager.registerListener('destroy_elems', this, 'destroy');

  //events
  $el.on('click', {sender: this}, function (event) {
    event.preventDefault();
    var isSelected = group.isSelected();
    if (!is_multiselect) {
      var current = action_manager.createRequest('get_current_selected').trigger();
      if (!!current && current !== group && !isSelected) {
        current.setSelected(false, true);
      }
    }
    var success = group.setSelected(!isSelected);
    if (success) {
      action_manager.createAction('set_view_current_multilist').trigger(event.data.sender);
    }
    event.stopPropagation();
  });

  $el.find('.mlc-collapser').on('click', function (event) {
    event.preventDefault();
    var text = $(this).text();
    text = (text == 'up' ? 'down' : 'up');
    $(this).text(text);
    var title = $(this).attr('title');
    title = (title == 'Свернуть группу' ? 'Развернуть группу' : 'Свернуть группу');
    $(this).attr('title', title);
    $(this).toggleClass('opened closed');
    $el.find('.ingroup').toggleClass('hidden');
    event.stopPropagation();
    return false;
  });

  this.onModelSelected = function (selected) {
    if (selected) {
      $el.addClass('selected');
    } else {
      $el.removeClass('selected');
    }
  };

  this.focus = function () {
    $el.addClass('focused');
    $el.prepend('<input class="focuser" type="checkbox">').find('input.focuser').focus();
    $el.find('input.focuser').detach();
    action_manager.createAction('focus_input').trigger();
  };

  this.unfocus = function () {
    $el.removeClass('focused');
  };

  this.destroy = function () {
    $el.off('click');
    $el.find('.mlc-collapser').off('click');
    delete $el;
//    $el.empty();
//    $el.detach();
  };

  return this;
};

MultiControl.Views.CharacterView = function (model, parentsString, isMultiselect, actionManager) {
  var character = model,
      is_multiselect = isMultiselect,
      $el = $('<li class="character"><span class="title"></span></li>'),
      parents = (parentsString == '' ? model.getName() : parentsString + ' - ' + model.getName()),
      sender = this,
      action_manager = actionManager;
  $el.attr('character-id', model.getId());
  $el.find('.title').text(character.getName());
  $el.find('.title').attr('title', parents);

  //methods
  this.getEl = function () {
    return $el;
  };
  this.getModel = function () {
    return character;
  };
  //listeners
  action_manager.registerListener('character_selected_' + character.getId(), this, 'onModelSelected');
  action_manager.registerListener('destroy_elems', this, 'destroy');

  //events
  $el.on('click', {sender: this}, function (event) {
    event.preventDefault();
    var isSelected = character.isSelected();
    if (!is_multiselect) {
      var current = action_manager.createRequest('get_current_selected').trigger();
      if (!!current && current !== character && !isSelected) {
        current.setSelected(false, true);
      }
    }
    var success = character.setSelected(!isSelected);
    if (success) {
      action_manager.createAction('set_view_current_multilist').trigger(event.data.sender);
    }
    event.stopPropagation();
  });

  this.onModelSelected = function (selected) {
    if (selected) {
      $el.addClass('selected');
    } else {
      $el.removeClass('selected');
    }
  };

  this.focus = function () {
    $el.addClass('focused');
    $el.prepend('<input class="focuser" type="checkbox">').find('input.focuser').focus();
    $el.find('input.focuser').detach();
    action_manager.createAction('focus_input').trigger();
  };

  this.unfocus = function () {
    $el.removeClass('focused');
  };

  this.destroy = function () {
    $el.off('click');
    delete $el;
//    $el.empty();
//    $el.detach();
  };

  return this;
};

MultiControl.Views.MultilistView = function (el, isMultiselect, actionManager) {
  var $el = el,
      handle,
      is_multiselect = isMultiselect,
      views = [],
      current_view = -1,
      action_manager = actionManager;
  action_manager.registerListener('open_multilist', this, 'openMultilist');
  action_manager.registerListener('close_multilist', this, 'closeMultilist');
  action_manager.registerListener('add_multilist_view', this, 'addView');
  action_manager.registerListener('set_view_current_multilist', this, 'setViewCurrent');
  action_manager.registerRequest('multilist_is_hidden', this, 'isHidden');
  action_manager.registerListener('destroy_elems', this, 'destroy');

  this.openMultilist = function () {
    $el.removeClass('hidden');
    current_view = -1;
    action_manager.registerListener('focus_current_list_down', this, 'focusCurrentListDown');
    action_manager.registerListener('focus_current_list_up', this, 'focusCurrentListUp');
    action_manager.registerListener('focus_current_list_top', this, 'focusCurrentListTop');
    action_manager.registerListener('focus_current_list_bottom', this, 'focusCurrentListBottom');

    action_manager.registerListener('close_current_list', this, 'closeMultilist');

    action_manager.registerListener('add_current_word', this, 'addCurrentWord');

    if (!!views[current_view]) {
      views[current_view].unfocus();
    }
  };
  this.closeMultilist = function () {
    $el.addClass('hidden');
    action_manager.unregisterListener('focus_current_list_down', this);
    action_manager.unregisterListener('focus_current_list_up', this);
    action_manager.unregisterListener('focus_current_list_bottom', this);
    action_manager.unregisterListener('focus_current_list_top', this);

    action_manager.unregisterListener('close_current_list', this);
    action_manager.unregisterListener('add_current_word', this);
  };
  this.focusCurrentListDown = function () {
    if (!!views[current_view]) {
      views[current_view].unfocus();
    }
    if (current_view < views.length - 1) {
      current_view++;
    }
    views[current_view].focus();
  };
  this.focusCurrentListUp = function () {
    if (!!views[current_view]) {
      views[current_view].unfocus();
    }
    if (current_view > 0) {
      current_view--;
    }
    views[current_view].focus();
  };
  this.focusCurrentListTop = function () {
    if (!!views[current_view]) {
      views[current_view].unfocus();
    }
    current_view = 0;
    views[current_view].focus();
  };
  this.focusCurrentListBottom = function () {
    if (!!views[current_view]) {
      views[current_view].unfocus();
    }
    current_view = views.length - 1;
    views[current_view].focus();
  };
  this.addView = function (view) {
    views.push(view);
  };
  this.addCurrentWord = function () {
    if (!!views[current_view]) {
      var model = views[current_view].getModel();
      var isSelected = model.isSelected();
      if (!is_multiselect) {
        var current = action_manager.createRequest('get_current_selected').trigger();
        if (!!current && current !== model && !isSelected) {
          current.setSelected(false, true);
        }
      }
      model.setSelected(!isSelected);
      action_manager.createAction('clear_input').trigger();
    }
  };
  this.setViewCurrent = function (view) {
    for (var i = 0; i < views.length; i++) {
      if (views[i] === view) {
        if (!!views[current_view]) {
          views[current_view].unfocus();
        }
        current_view = i;
        view.focus();
      }
    }
  };
  this.isHidden = function () {
    return $el.hasClass('hidden');
  };
  $el.on('click', function (event) {
    event.preventDefault();
    event.stopPropagation();
  });
  this.destroy = function () {
    $el.off('click');
    views = [];
    $el.empty();
    delete $el;
//    $el.detach();
  }
};

MultiControl.Views.AutocompleteLiView = function (data, isMultiselect, actionManager) {
  var model = data,
      is_multiselect = isMultiselect,
      $el = $('<li class="mlc-autocomplete-li"><span class="title"></span></li>'),
      action_manager = actionManager;
  $el.find('.title').text(model.getName());
  this.getEl = function () {
    return $el;
  };
  this.getModel = function () {
    return model;
  };
  this.clear = function () {
    $el.off('click');
  };
  this.focus = function () {
    $el.addClass('focused');
  };

  this.unfocus = function () {
    $el.removeClass('focused');
  };

  $el.on('click', {sender: this}, function (event) {
    event.preventDefault();
    var isSelected = model.isSelected();
    if (!is_multiselect) {
      var current = action_manager.createRequest('get_current_selected').trigger();
      if (!!current && current !== model && !isSelected) {
        current.setSelected(false, true);
      }
    }
    model.setSelected(!isSelected);
    action_manager.createAction('clear_input').trigger();
    action_manager.createAction('focus_input').trigger();
    action_manager.createAction('close_autocomplete').trigger();
    action_manager.createAction('open_multilist').trigger();
    action_manager.createAction('hide_all_groups').trigger();
    event.stopPropagation();
  });

  this.destroy = function () {
    this.clear();
    delete $el;
    $el.empty();
  }
};

MultiControl.Views.AutocompleteView = function (el, isMultiselect, actionManager) {
  var $el = el,
      is_multiselect = isMultiselect,
      views = [],
      current_view = -1,
      action_manager = actionManager;
  action_manager.registerListener('open_autocomplete', this, 'openAutocomplete');
  action_manager.registerListener('close_autocomplete', this, 'closeAutocomplete');
  action_manager.registerRequest('autocomplete_is_hidden', this, 'isHidden');
  action_manager.registerListener('set_view_current_autocomplete', this, 'setViewCurrent');
  action_manager.registerListener('destroy_elems', this, 'destroy');

  this.openAutocomplete = function (data) {
    this.draw(data);
    current_view = -1;
    $el.removeClass('hidden');
    action_manager.registerListener('focus_current_list_down', this, 'focusCurrentListDown');
    action_manager.registerListener('focus_current_list_up', this, 'focusCurrentListUp');
    action_manager.registerListener('focus_current_list_top', this, 'focusCurrentListTop');
    action_manager.registerListener('focus_current_list_bottom', this, 'focusCurrentListBottom');

    action_manager.registerListener('close_current_list', this, 'closeAutocomplete');

    action_manager.registerListener('add_current_word', this, 'addCurrentWord');
  };
  this.closeAutocomplete = function () {
    $el.addClass('hidden');
    action_manager.unregisterListener('focus_current_list_down', this);
    action_manager.unregisterListener('focus_current_list_up', this);
    action_manager.unregisterListener('focus_current_list_bottom', this);
    action_manager.unregisterListener('focus_current_list_top', this);

    action_manager.unregisterListener('close_current_list', this);

    action_manager.unregisterListener('add_current_word', this);
  };
  this.focusCurrentListDown = function () {
    if (!!views[current_view]) {
      views[current_view].unfocus();
    }
    if (current_view < views.length - 1) {
      current_view++;
    }
    views[current_view].focus();
  };
  this.focusCurrentListUp = function () {
    if (!!views[current_view]) {
      views[current_view].unfocus();
    }
    if (current_view > 0) {
      current_view--;
    }
    views[current_view].focus();
  };
  this.focusCurrentListTop = function () {
    if (!!views[current_view]) {
      views[current_view].unfocus();
    }
    current_view = 0;
    views[current_view].focus();
  };
  this.focusCurrentListBottom = function () {
    if (!!views[current_view]) {
      views[current_view].unfocus();
    }
    current_view = views.length - 1;
    views[current_view].focus();
  };
  this.addCurrentWord = function () {
    if (!!views[current_view]) {
      var model = views[current_view].getModel();
      var isSelected = model.isSelected();
      if (!is_multiselect) {
        var current = action_manager.createRequest('get_current_selected').trigger();
        if (!!current && current !== model && !isSelected) {
          current.setSelected(false, true);
        }
      }
      model.setSelected(!isSelected);
      action_manager.createAction('clear_input').trigger();
      action_manager.createAction('close_autocomplete').trigger();
      action_manager.createAction('open_multilist').trigger();
      action_manager.createAction('hide_all_groups').trigger();
    }
  };
  this.draw = function (data) {
    this.clear();
    for (var i = 0, max = data.length; i < max; i++) {
      var view = new MultiControl.Views.AutocompleteLiView(data[i], is_multiselect, action_manager);
      $el.find('ul').append(view.getEl());
      views.push(view);
    }
  };
  this.setViewCurrent = function (view) {
    for (var i = 0; i < views.length; i++) {
      if (views[i] === view) {
        if (!!views[current_view]) {
          views[current_view].unfocus();
        }
        current_view = i;
        view.focus();
      }
    }
  };
  this.clear = function () {
    for (var i = 0, max = views.length; i < max; i++) {
      views[i].clear();
    }
    $el.find('ul').empty();
    views = [];
  };
  this.isHidden = function () {
    return $el.hasClass('hidden');
  };
  this.destroy = function () {
    for (var i = 0, max = views.length; i < max; i++) {
      views[i].destroy();
    }
    views = [];
    delete current_view;
    $el.empty();
    $el.detach();
    delete $el;
//    $el.empty();
  }
};

MultiControl.Views.InputTagView = function (tag) {
  var model = tag,
      $el = $('<div class="mlc-tag"><span class="name"></span><span class="close lsf"> close</span></div>');

  $el.find('.name').attr('id', 'tag_' + tag.getId()).text(tag.getName()).attr('title', tag.getName());

  this.getEl = function () {
    return $el;
  };
  this.getModel = function () {
    return model;
  };

  this.clearView = function () {
    $el.find('.close').off('click');
  };

  $el.find('.close').on('click', function (event) {
    event.preventDefault();
    model.setSelected(false);
    event.stopPropagation();
  });

  return this;
};

MultiControl.Views.InputView = function (input, inputmodel, actionManager) {
  var $el = input,
      model = inputmodel,
      tag_views = [],
      action_manager = actionManager,
      disabled = false;
  action_manager.registerListener('update_input', this, 'updateInput');
  action_manager.registerListener('focus_input', this, 'focusInput');
  action_manager.registerListener('clear_input', this, 'clearInput');
  action_manager.registerListener('set_input_value', this, 'setInputValue');
  action_manager.registerListener('disable', this, 'setDisabled');
  action_manager.registerListener('enable', this, 'setEnabled');
  action_manager.registerListener('destroy_elems', this, 'destroy');

  this.updateInput = function () {
    var selectedItems = model.getSelectedItems();
    this.clearTags();
    var tags = $el.find('.mlc-tags');
    for (var i = 0; i < selectedItems.length; i++) {
      var item = selectedItems[i];
      var tag_view = new MultiControl.Views.InputTagView(item);
      var tags_els = tags.find('.mlc-tag');
      var tags_length = tags_els.length;
      if (tags_length == 0) {
        tags.prepend(tag_view.getEl());
      }
      else {
        tags_els.eq(tags_length - 1).after(tag_view.getEl());
      }
      tag_views.push(tag_view);
    }
  };

  this.clearTags = function () {
    for (var i = 0; i < tag_views.length; i++) {
      tag_views[i].clearView();
      tag_views[i].getEl().detach();
    }
    tag_views = [];
  };

  this.focusInput = function () {
    $el.find('input.mlc-taginput').focus();
  };

  this.clearInput = function () {
    $el.find('input.mlc-taginput').val('');
  };

  this.setInputValue = function (value) {
    $el.find('input.mlc-taginput').val(value);
  };

  this.setDisabled = function () {
    disabled = true;
    $el.find('input').prop('disabled', true);
  };

  this.setEnabled = function () {
    disabled = false;
    $el.find('input').prop('disabled', false);
  };
  $el.on('click', function (event) {
    if (!disabled) {
      if (action_manager.createRequest('autocomplete_is_hidden').trigger() &&
          action_manager.createRequest('multilist_is_hidden').trigger()) {
        action_manager.createAction('close_autocomplete').trigger();
        action_manager.createAction('open_multilist').trigger();
        action_manager.createAction('hide_all_groups').trigger();
      }
      $el.find('.mlc-taginput').focus();
      $(this).addClass('focused');
      event.preventDefault();
      event.stopPropagation();
    }
  });

  $el.find('input').on('keydown', function (event) {
    if (!disabled) {
      var position = $(this).getCursorPosition();
      if (position == 0 && $(this).val() == '' && event.keyCode == 8) {
        var model = tag_views[tag_views.length - 1].getModel();
        model.setSelected(false);
        action_manager.createAction('set_input_value').trigger(model.getName());
      }
    }
  });

  $el.find('input').on('keyup', function (event) {
    if (!disabled) {
      var code = event.keyCode,
          action_string = '';
      switch (code) {
        //down
        case 40:
          action_string = 'focus_current_list_down';
          break;
        case 38:
          action_string = 'focus_current_list_up';
          break;
        case 34:
          action_string = 'focus_current_list_bottom';
          break;
        case 33:
          action_string = 'focus_current_list_top';
          break;
        case 13:
          action_string = 'add_current_word';
          break;
        default:
          var word = $(this).val();
          var data = action_manager.createRequest('get_word').trigger(word);
          if (!!data) {
            action_manager.createAction('close_multilist').trigger();
            action_manager.createAction('open_autocomplete').trigger(data);
            action_manager.createAction('hide_all_groups').trigger();
          }
          if (word == '') {
            action_manager.createAction('close_autocomplete').trigger();
            action_manager.createAction('open_multilist').trigger();
            action_manager.createAction('hide_all_groups').trigger();
          }
          break;
      }
      if (action_string != '') {
        action_manager.createAction(action_string).trigger();
      }
    }
  });

  this.destroy = function () {
    this.clearTags();
    $el.off('click');
    $el.find('input').off('keyup');
    $el.find('input').off('keydown');
    delete $el;
//    $el.empty();
//    $el.detach();
  }
};

MultiControl.Views.AllGroupsView = function (all_groups, el, actionManager, implicitgroups) {
  var $el = el,
      template = '<li></li>',
      model = all_groups,
      implicit_groups = implicitgroups;

  actionManager.registerListener('update_input', this, 'updateGroups');
  actionManager.registerListener('show_all_groups', this, 'showAllGroups');
  actionManager.registerListener('hide_all_groups', this, 'hideAllGroups');
  actionManager.registerListener('destroy_elems', this, 'destroy');


  this.getEl = function () {
    return $el;
  };

  this.updateGroups = function () {
    model.updateGroups();
    var groups = model.getAllGroups();
    $el.find('.mlc-clickable-in-all-groups').find('a').off('click');
    $el.empty();

    if (groups.length > 0) {
      if (implicit_groups == 'parents') {
        $el.append('<li>Также имплицитно добавлен в группы:</li>');
      } else if (implicit_groups == 'children') {
        $el.append('<li>В выбранные группы также добавлены персонажи:</li>');
      }
      this.draw(groups);
    }
  };

  this.showAllGroups = function () {
    $el.removeClass('hidden');
  };

  this.hideAllGroups = function () {
    $el.addClass('hidden');
  };

  this.draw = function (models) {
    var border = Math.min(models.length, 3);
    for (var i = 0; i < models.length; i++) {
      if (i == border) {
        var clickable_li = $('<li class="mlc-clickable-in-all-groups"><a href="#" class="hiding">далее...</a></li>')
        $el.append(clickable_li);
        clickable_li.find('a').on('click', function (event) {
          event.preventDefault();
          $(this).toggleClass('hiding');
          if ($(this).hasClass('hiding')) {
            $(this).text('далее ...');
          } else {
            $(this).text('меньше ...');
          }
          $('.mlc-showable').toggleClass('hidden');
          event.stopPropagation();
        });
      }
      var text = models[i].getName();
      if (i != models.length - 1) {
        text += ',';
      }
      var li = $(template).text(text);
      $el.append(li);

      if (i >= border) {
        li.addClass('hidden mlc-showable');
      }
    }
  };

  this.destroy = function () {
    $el.find('.mlc-clickable-in-all-groups').find('a').off('click');
    $el.empty();
    delete $el;
//    $el.empty();
//    $el.detach();
  }
};


