$(document).ready(() => {

    var filterInput = document.querySelector('input[name=filterTags]');
    filterTagify = new Tagify(filterInput, {
        enforceWhitelist: true,
        maxTags: 10,
        dropdown: {
            maxItems: 20,
            classname: "tags-look",
            enabled: 0,
            closeOnSelect: false
        }
    });
    filterTagify
        .on('input', onFilterInput)
        .on('change', onFilterTagChange);
    function onFilterTagChange(e) {
        let tags = [];
        if (!(filterInput.value === "")) {
            let tagsstr = JSON.parse(filterInput.value);
            tags = tagsstr.map(t => t.value);
        }

        hubConnection.invoke("GetGamesByTags", tags);
    }
    function onFilterInput(e) {

          filterTagify.settings.whitelist.length = 0; // reset current whitelist
          filterTagify.loading(true).dropdown.hide.call(filterTagify); // show the loader animation

        $.when(
            $.get("Home/GetTags", function (data, statusText) {
                let newWhitelist = data;
                filterTagify.settings.whitelist.push(...newWhitelist, ...filterTagify.value)

            })).then(function () {
                filterTagify.loading(false).dropdown.show.call(filterTagify, e.detail.value);
            });
    }


    var gameInput = document.querySelector('input[name=gameTags]')
    gameTagify = new Tagify(gameInput);
    gameTagify.on('input', onGameInput)
    function onGameInput(e) {

        gameTagify.settings.whitelist.length = 0; // reset current whitelist
        gameTagify.loading(true).dropdown.hide.call(gameTagify); // show the loader animation

        $.when(
            $.get("Home/GetTags", function (data, statusText) {
                let newWhitelist = data;
                gameTagify.settings.whitelist.push(...newWhitelist, ...gameTagify.value)

            })).then(function () {
                gameTagify.loading(false).dropdown.show.call(gameTagify, e.detail.value);
            });
    }

    const hubConnection = new signalR.HubConnectionBuilder()
        .withUrl("/lobby")
        .build();

    hubConnection.start().then(() => {
        hubConnection.invoke("RegistrateUser", userName);
        hubConnection.invoke("GetAllAvailableGames");
    });

    $("#gameList").on("click", function (event) {
        var target = $(event.target);
        if (target.is('.btn')) {
            hubConnection.invoke("joinGame", event.target.id);
        }
    });

    $("#openCreateGameModal").click(function () {
        $("#createGameModal").modal('show');
    });

    $("#createGameButton").click(function () {
        var tags = gameInput.value;
        var tagsstr = JSON.parse(gameInput.value);
        tags = tagsstr.map(t => t.value);
        var gameName = $("#gameName").val();

        $.ajax({
            type: 'POST',
            url: 'Home/SaveTags',
            contentType: 'application/json',
            data: JSON.stringify(tags)
        });

        hubConnection.invoke("CreateGame", tags, gameName);
    });

    hubConnection.on("updateGameList", function (games) {
        let gameList = $("#gameList");
        gameList.html('');
        for (let i = 0; i < games.length; ++i) {
            gameList.append("<div class=\"card\">" +
                "<div class=\"card-body\">" +
                "<h4>" + games[i].gameName + "</h4>" +
                "<h5 class=\"card-title\"> <b>" + games[i].creatorName + "</b> is waiting for an opponent.</h5>" +
                "<button type=\"button\" class=\"btn btn-primary joinBtn mb-3\" id=\"" + games[i].gameId + "\" >Join</button>" +
                "<div id=\"tags" + games[i].gameId + "\"> </div>" +
                "</div></div>");

            for (let j = 0; j < games[i].tags.length; ++j) {
                if (j != games[i].tags.length - 1) {
                    $("#tags" + games[i].gameId).append("<span class=\"tag\"> #" + games[i].tags[j] + ",</span>");
                } else {
                    $("#tags" + games[i].gameId).append("<span class=\"tag\"> #" + games[i].tags[j] + ".</span>");
                }
            }
        }
    });

    hubConnection.on("gamesFound", function (games) {

        let gameList = $("#gameList");
        for (let i = 0; i < games.length; ++i) {
            gameList.append("<div class=\"card\">" +
                "<div class=\"card-body\">" +
                "<h4>" + games[i].gameName + "</h4>" +
                "<h5 class=\"card-title\"> <b>" + games[i].creatorName + "</b> is waiting for an opponent.</h5>" +
                "<button type=\"button\" class=\"btn btn-primary joinBtn mb-3\" id=\"" + games[i].gameId + "\" >Join</button>" +
                "<div id=\"tags" + games[i].gameId + "\"> </div>"+
                "</div></div>");

            for (let j = 0; j < games[i].tags.length; ++j) {
                if (j != games[i].tags.length - 1) {
                    $("#tags" + games[i].gameId).append("<span class=\"tag\"> #" + games[i].tags[j] + ",</span>");
                } else {
                    $("#tags" + games[i].gameId).append("<span class=\"tag\"> #" + games[i].tags[j] + ".</span>");
                }
            }
        }

    });

    hubConnection.on("redirectToWaitingScreen", function (gameId) {
        window.location.href = gameUrl + "/" + gameId;
    })

    hubConnection.on("gameIsClosed", function (gameId) {
        $("#" + gameId + "").closest("div.card").hide();
    });

    hubConnection.on("connectedToTheGame", function (gameId) {
        window.location.href = gameUrl + "/" + gameId;
    })

    hubConnection.on("newGameCreated", function (game) {
        let gameList = $("#gameList");
        gameList.append("<div class=\"card\">" +
            "<div class=\"card-body\">" +
            "<h5 class=\"card-title\">" + game.creatorName + " is waiting for an opponent.</h5>" +
            "<button type=\"button\" class=\"btn btn-primary\" id=\"" + game.gameId + "\" >Join</button>" +
            "</div></div>");
    })

});
