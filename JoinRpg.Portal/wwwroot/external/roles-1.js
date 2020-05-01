(function () {
    function convertCharacterToElement(character) {
        var el = document.createElement("div");
        el.classList = "joinrpg-roles joinrpg-character";
        var claimLink = character.ClaimLink ? " <a href=\"" + character.ClaimLink + "\">заявиться</a> " : "";
        el.innerHTML = "<b>" + character.CharacterName + "</b>" + claimLink + character.Description;
        return el;
    }

    function callServer(uri, onResult) {
        var xmlhttp = ("onload" in new XMLHttpRequest()) ? new XMLHttpRequest() : new XDomainRequest();

        xmlhttp.open("GET", uri, true);
        xmlhttp.onreadystatechange = function () {
            if (xmlhttp.readyState === 4) {
                if (xmlhttp.status === 200) {
                    onResult(xmlhttp.responseText);
                }
            }
        };
        xmlhttp.send(null);
    }

    function loadHotRoles(element) {
        var projectId = element.dataset.project;
        var characterGroupId = element.dataset.charactergroup;
        if (!projectId || !characterGroupId) {
            return;
        }
        var server = element.dataset.server;
        if (!server) {
            server = "http://joinrpg.ru";
        }
        var maxCount = element.dataset.maxcount;
        var uri = server + "/" + projectId + "/roles/" + characterGroupId + "/hotjson?maxcount=" + maxCount;

        var onResult = function(result) {
            if (!result) {
                return;
            }
            var jsonResult = JSON.parse(result);
            while (element.firstChild) {
                element.removeChild(element.firstChild);
            }
            for (var index = 0; index < jsonResult.length; ++index) {
                element.appendChild(convertCharacterToElement(jsonResult[index]));
            }
        }

        callServer(uri, onResult);

    }

    var elements = document.getElementsByClassName("joinrpg-hot-roles");
    var index, len;

    for (index = 0, len = elements.length; index < len; ++index)
    {
        loadHotRoles(elements[index]);
    }

})();