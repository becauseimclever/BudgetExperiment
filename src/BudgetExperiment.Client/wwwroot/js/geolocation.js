// Geolocation JavaScript interop module for GPS capture

/**
 * Gets the current GPS position using the browser Geolocation API.
 * @returns {Promise<{latitude: number, longitude: number}>} The coordinates.
 * @throws {Error} If geolocation is denied or unavailable.
 */
export function getCurrentPosition() {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject(new Error('Geolocation is not supported by this browser.'));
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => {
                resolve({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                });
            },
            (error) => {
                switch (error.code) {
                    case error.PERMISSION_DENIED:
                        reject(new Error('Location permission denied.'));
                        break;
                    case error.POSITION_UNAVAILABLE:
                        reject(new Error('Location information is unavailable.'));
                        break;
                    case error.TIMEOUT:
                        reject(new Error('Location request timed out.'));
                        break;
                    default:
                        reject(new Error('An unknown error occurred getting location.'));
                        break;
                }
            },
            {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 60000
            }
        );
    });
}

/**
 * Checks if the browser supports the Geolocation API.
 * @returns {boolean} True if geolocation is supported.
 */
export function isSupported() {
    return 'geolocation' in navigator;
}
