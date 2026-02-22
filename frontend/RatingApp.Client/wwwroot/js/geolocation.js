window.getGeolocation = function () {
    return new Promise(function (resolve, reject) {
        if (!navigator.geolocation) {
            reject('Geolocation is not supported by your browser.');
            return;
        }
        navigator.geolocation.getCurrentPosition(
            function (pos) {
                resolve({ latitude: pos.coords.latitude, longitude: pos.coords.longitude });
            },
            function (err) {
                reject(err.message);
            },
            { enableHighAccuracy: true, timeout: 10000 }
        );
    });
};