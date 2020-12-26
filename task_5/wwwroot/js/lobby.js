$(document).ready(() => {
    const hubConnection = new signalR.HubConnectionBuilder()
        .withUrl("/lobby")
        .build();

    hubConnection.start().then(() => {
        setTimeout(x => {
            hubConnection.invoke("GetAllAvailableGames");
        }, 1);
    });

    hubConnection.on("gamesFound", function (msg) {

        alert(msg);

    });

    $("#createGame").click(function () {
        hubConnection.invoke("CreateGame");
    });

    hubConnection.on("gameCreate", function () {
        alert('game created');
    })

});
