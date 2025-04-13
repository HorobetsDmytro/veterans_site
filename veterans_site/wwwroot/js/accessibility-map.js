let map;
let markers = [];
let userMarkers = [];
let markerLayer;
let addMarkerMode = false;
let currentPosition = null;

function initMap() {
    console.log('Initializing map...');

    const ukraineCenter = { lat: 49.0, lng: 31.0 };

    map = L.map('accessibilityMap').setView([ukraineCenter.lat, ukraineCenter.lng], 6);

    console.log('Map created:', map);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 19
    }).addTo(map);

    markerLayer = L.layerGroup().addTo(map);

    document.querySelectorAll('.filter-checkbox').forEach(checkbox => {
        checkbox.checked = false;
    });
    
    loadMarkers();

    console.log('Checking filters after initialization...');
    const filterElements = document.querySelectorAll('.filter-checkbox');
    filterElements.forEach(checkbox => {
        console.log(`Filter "${checkbox.id}" is ${checkbox.checked ? 'checked' : 'unchecked'}`);
    });

    if (document.getElementById('userMarkersList')) {
        loadUserMarkers();
    }

    map.on('click', function(event) {
        console.log('Map clicked:', event.latlng);
        if (addMarkerMode) {
            console.log('Add marker mode is ON');
            showAddMarkerModal(event.latlng);
        } else {
            console.log('Add marker mode is OFF');
            toggleAddMarkerMode();
            showAddMarkerModal(event.latlng);
        }
    });

    L.control.zoom({
        position: 'topright'
    }).addTo(map);

    initEventHandlers();
}

function toggleAddMarkerMode() {
    addMarkerMode = !addMarkerMode;

    if (addMarkerMode) {
        document.getElementById('addMarkerModeBtn').classList.add('active');
        document.getElementById('addMarkerHelp').classList.remove('d-none');
    } else {
        document.getElementById('addMarkerModeBtn').classList.remove('active');
        document.getElementById('addMarkerHelp').classList.add('d-none');
    }

    console.log('Toggle addMarkerMode value:', addMarkerMode);
}

function loadMarkers() {
    fetch('/AccessibilityMap/GetMarkers')
        .then(response => response.json())
        .then(data => {
            markers = data;
            console.log('Loaded markers:', markers.length);
            renderMarkers();
        })
        .catch(error => console.error('Помилка завантаження маркерів:', error));
}

function loadUserMarkers() {
    console.log("Спроба завантаження маркерів користувача");

    const userMarkersList = document.getElementById('userMarkersList');
    if (!userMarkersList) {
        console.log("Елемент userMarkersList не знайдено");
        return;
    }

    userMarkersList.innerHTML = '<p class="text-center"><i class="fas fa-spinner fa-spin"></i> Завантаження...</p>';

    fetch('/AccessibilityMap/GetMarkersByUser')
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            console.log("Отримані маркери користувача:", data);
            userMarkers = data;
            renderUserMarkersList();
        })
        .catch(error => {
            console.error('Помилка завантаження маркерів користувача:', error);
            userMarkersList.innerHTML =
                '<p class="text-center text-danger">Помилка завантаження маркерів</p>';
        });
}

function renderMarkers() {
    markerLayer.clearLayers();
    console.log('Очищено шар маркерів');

    const filteredMarkers = filterMarkers(markers);
    console.log(`Після фільтрації залишилось ${filteredMarkers.length} маркерів з ${markers.length}`);

    filteredMarkers.forEach(markerData => {
        addMarkerToMap(markerData);
    });
}

function addMarkerToMap(markerData) {

    const markerIcon = L.divIcon({
        className: 'custom-marker',
        html: `<div style="background-color: #2e51a2; width: 20px; height: 20px; border-radius: 50%; border: 2px solid white;"></div>`,
        iconSize: [20, 20],
        iconAnchor: [10, 10]
    });

    const marker = L.marker([markerData.latitude, markerData.longitude], {
        icon: markerIcon,
        title: markerData.title
    }).addTo(markerLayer);

    marker.on('click', function() {
        showMarkerDetails(markerData.id);
    });

    return marker;
}

document.addEventListener('DOMContentLoaded', function() {
    window.copyToClipboard = copyToClipboard;

    const detailsModal = document.getElementById('markerDetailsModal');
    if (detailsModal) {
        detailsModal.addEventListener('shown.bs.modal', function() {
            const routeBtn = detailsModal.querySelector('.route-btn');
            if (routeBtn) {
                const lat = routeBtn.getAttribute('data-lat');
                const lng = routeBtn.getAttribute('data-lng');
                if (lat && lng) {
                    routeBtn.href = getDirectionsUrl(lat, lng);
                }
            }
        });
    }
});

function filterMarkers(markersData) {
    const showRamp = document.getElementById('filterRamp')?.checked || false;
    const showBlind = document.getElementById('filterBlind')?.checked || false;
    const showElevator = document.getElementById('filterElevator')?.checked || false;
    const showToilet = document.getElementById('filterToilet')?.checked || false;
    const showParking = document.getElementById('filterParking')?.checked || false;

    console.log('Filters:', { showRamp, showBlind, showElevator, showToilet, showParking });

    const anyFilterActive = showRamp || showBlind || showElevator || showToilet || showParking;

    if (!anyFilterActive) {
        return markersData;
    }

    return markersData.filter(marker => {
        if (showRamp && !marker.hasRamp) return false;
        if (showBlind && !marker.hasBlindSupport) return false;
        if (showElevator && !marker.hasElevator) return false;
        if (showToilet && !marker.hasAccessibleToilet) return false;
        if (showParking && !marker.hasParking) return false;

        return true;
    });
}

function initFilterHandlers() {
    const filterElements = document.querySelectorAll('.filter-checkbox');
    if (filterElements.length === 0) {
        console.warn('Фільтри не знайдено на сторінці');
        return;
    }

    console.log('Знайдено фільтрів:', filterElements.length);

    filterElements.forEach(checkbox => {
        checkbox.checked = false;
        console.log(`Фільтр "${checkbox.id}" вимкнений`);
    });

    filterElements.forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            console.log(`Filter "${this.id}" changed to ${this.checked}`);
            renderMarkers(); 
        });
    });
}

function showMarkerDetails(markerId) {
    fetch(`/AccessibilityMap/GetMarkerDetails?id=${markerId}`)
        .then(response => response.text())
        .then(html => {
            document.getElementById('markerDetailsContent').innerHTML = html;

            const canEdit = checkEditPermission(markerId);

            if (canEdit) {
                document.getElementById('editMarkerBtn').classList.remove('d-none');
                document.getElementById('deleteMarkerBtn').classList.remove('d-none');

                document.getElementById('editMarkerBtn').onclick = function() {
                    editMarker(markerId);
                };

                document.getElementById('deleteMarkerBtn').onclick = function() {
                    deleteMarker(markerId);
                };
            } else {
                document.getElementById('editMarkerBtn').classList.add('d-none');
                document.getElementById('deleteMarkerBtn').classList.add('d-none');
            }

            const routeBtn = document.querySelector('.route-btn');
            if (routeBtn) {
                const lat = routeBtn.getAttribute('data-lat');
                const lng = routeBtn.getAttribute('data-lng');
                if (lat && lng) {
                    routeBtn.href = getDirectionsUrl(lat, lng);
                }
            }

            const copyBtn = document.querySelector('.copy-coords-btn');
            if (copyBtn) {
                const lat = copyBtn.getAttribute('data-lat');
                const lng = copyBtn.getAttribute('data-lng');
                copyBtn.onclick = function() {
                    copyToClipboard(`${lat}, ${lng}`);
                    return false;
                };
            }

            new bootstrap.Modal(document.getElementById('markerDetailsModal')).show();
        })
        .catch(error => console.error('Помилка завантаження деталей маркера:', error));
}

function checkEditPermission(markerId) {
    const userId = document.querySelector('meta[name="user-id"]')?.content;
    const isAdmin = document.querySelector('meta[name="is-admin"]')?.content === 'true';

    if (isAdmin) return true;

    const marker = userMarkers.find(m => m.id === markerId);
    return marker !== undefined;
}

function editMarker(markerId) {
    bootstrap.Modal.getInstance(document.getElementById('markerDetailsModal')).hide();

    fetch(`/AccessibilityMap/GetMarkerDetails?id=${markerId}&raw=true`)
        .then(response => response.json())
        .then(data => {
            document.getElementById('markerId').value = data.id;
            document.getElementById('markerLat').value = data.latitude;
            document.getElementById('markerLng').value = data.longitude;
            document.getElementById('markerTitle').value = data.title;
            document.getElementById('markerDescription').value = data.description;
            document.getElementById('markerAddress').value = data.address;
            document.getElementById('markerHasRamp').checked = data.hasRamp;
            document.getElementById('markerHasBlindSupport').checked = data.hasBlindSupport;
            document.getElementById('markerHasElevator').checked = data.hasElevator;
            document.getElementById('markerHasAccessibleToilet').checked = data.hasAccessibleToilet;
            document.getElementById('markerHasParking').checked = data.hasParking;

            document.getElementById('markerModalLabel').textContent = 'Редагувати місце';

            new bootstrap.Modal(document.getElementById('markerModal')).show();
        })
        .catch(error => console.error('Помилка завантаження даних маркера:', error));
}

function deleteMarker(markerId) {
    if (confirm('Ви дійсно хочете видалити це місце?')) {
        fetch(`/AccessibilityMap/DeleteMarker?id=${markerId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    bootstrap.Modal.getInstance(document.getElementById('markerDetailsModal')).hide();

                    loadMarkers();

                    if (document.getElementById('userMarkersList')) {
                        loadUserMarkers();
                    }

                    showNotification('Місце успішно видалено!', 'success');
                } else {
                    showNotification('Помилка видалення місця!', 'danger');
                }
            })
            .catch(error => {
                console.error('Помилка видалення маркера:', error);
                showNotification('Помилка видалення місця!', 'danger');
            });
    }
}

function showAddMarkerModal(latLng) {
    addMarkerMode = false;
    document.getElementById('addMarkerModeBtn').classList.remove('active');
    document.getElementById('addMarkerHelp').classList.add('d-none');

    document.getElementById('markerForm').reset();
    document.getElementById('markerId').value = '0';
    document.getElementById('markerLat').value = latLng.lat;
    document.getElementById('markerLng').value = latLng.lng;

    document.getElementById('markerModalLabel').textContent = 'Додати нове місце';

    fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${latLng.lat}&lon=${latLng.lng}&addressdetails=1`)
        .then(response => response.json())
        .then(data => {
            if (data && data.display_name) {
                document.getElementById('markerAddress').value = data.display_name;
            }
        })
        .catch(error => {
            console.error('Помилка визначення адреси:', error);
        });

    new bootstrap.Modal(document.getElementById('markerModal')).show();
}

function saveMarker() {
    const formData = new FormData();
    formData.append('Id', document.getElementById('markerId').value);
    formData.append('Title', document.getElementById('markerTitle').value.trim());
    formData.append('Description', document.getElementById('markerDescription').value.trim());

    formData.append('Latitude', document.getElementById('markerLat').value);
    formData.append('Longitude', document.getElementById('markerLng').value);

    formData.append('Address', document.getElementById('markerAddress').value.trim());
    formData.append('HasRamp', document.getElementById('markerHasRamp').checked);
    formData.append('HasBlindSupport', document.getElementById('markerHasBlindSupport').checked);
    formData.append('HasElevator', document.getElementById('markerHasElevator').checked);
    formData.append('HasAccessibleToilet', document.getElementById('markerHasAccessibleToilet').checked);
    formData.append('HasParking', document.getElementById('markerHasParking').checked);

    formData.append('__RequestVerificationToken',
        document.querySelector('input[name="__RequestVerificationToken"]').value);

    const isNew = parseInt(document.getElementById('markerId').value) === 0;
    const url = isNew ? '/AccessibilityMap/AddMarker' :
        `/AccessibilityMap/UpdateMarker?id=${document.getElementById('markerId').value}`;
    const method = isNew ? 'POST' : 'PUT';

    fetch(url, {
        method: method,
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            console.log("Server response:", data);

            if (data.success) {
                bootstrap.Modal.getInstance(document.getElementById('markerModal')).hide();
                loadMarkers();
                if (document.getElementById('userMarkersList')) {
                    loadUserMarkers();
                }
                showNotification(isNew ? 'Місце успішно додано!' : 'Місце успішно оновлено!', 'success');
            } else {
                console.log("Validation errors:", data.errors);
                let errorMsg = 'Помилка збереження місця:';
                if (data.errors) {
                    data.errors.forEach(error => {
                        errorMsg += `\n- ${error.field}: ${error.errors.join(', ')}`;
                    });
                }
                showNotification(errorMsg, 'danger');
            }
        })
        .catch(error => {
            console.error('Error saving marker:', error);
            showNotification('Помилка збереження місця!', 'danger');
        });
}

function renderUserMarkersList() {
    const container = document.getElementById('userMarkersList');

    if (!container) {
        console.log("Контейнер userMarkersList не знайдено");
        return;
    }

    if (!userMarkers || userMarkers.length === 0) {
        console.log("Маркери користувача не знайдено або масив порожній");
        container.innerHTML = '<p class="text-center text-muted">У вас ще немає доданих місць</p>';
        return;
    }

    console.log(`Рендеринг ${userMarkers.length} маркерів користувача`);

    let html = '<div class="list-group">';

    userMarkers.forEach(marker => {
        html += `
            <div class="list-group-item user-marker-item" data-marker-id="${marker.id}">
                <div class="d-flex justify-content-between align-items-center">
                    <div>
                        <div class="fw-bold">${marker.title}</div>
                        <div class="small text-muted">${marker.address || 'Без адреси'}</div>
                    </div>
                </div>
            </div>
        `;
    });

    html += '</div>';
    container.innerHTML = html;

    document.querySelectorAll('.user-marker-item').forEach(item => {
        item.addEventListener('click', function() {
            const markerId = parseInt(this.dataset.markerId);
            const marker = userMarkers.find(m => m.id === markerId);

            if (marker) {
                map.setView([marker.latitude, marker.longitude], 15);

                showMarkerDetails(markerId);
            }
        });
    });
}

function initEventHandlers() {
    initFilterHandlers();

    document.querySelectorAll('.filter-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            renderMarkers();
        });
    });

    document.getElementById('resetFiltersBtn').addEventListener('click', function() {
        document.querySelectorAll('.filter-checkbox').forEach(checkbox => {
            checkbox.checked = false;
        });
        renderMarkers();
    });

    document.getElementById('addMarkerModeBtn').addEventListener('click', function() {
        console.log('Add marker button clicked');
        toggleAddMarkerMode();
    });

    const cancelAddMarkerBtn = document.getElementById('cancelAddMarkerBtn');
    if (cancelAddMarkerBtn) {
        cancelAddMarkerBtn.addEventListener('click', function() {
            addMarkerMode = false;
            document.getElementById('addMarkerModeBtn').classList.remove('active');
            document.getElementById('addMarkerHelp').classList.add('d-none');
        });
    }

    document.getElementById('saveMarkerBtn').addEventListener('click', saveMarker);

    document.getElementById('detectAddressBtn').addEventListener('click', function() {
        const lat = parseFloat(document.getElementById('markerLat').value);
        const lng = parseFloat(document.getElementById('markerLng').value);

        if (isNaN(lat) || isNaN(lng)) {
            showNotification('Неможливо визначити адресу!', 'danger');
            return;
        }

        fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&addressdetails=1`)
            .then(response => response.json())
            .then(data => {
                if (data && data.display_name) {
                    document.getElementById('markerAddress').value = data.display_name;
                } else {
                    showNotification('Неможливо визначити адресу!', 'danger');
                }
            })
            .catch(error => {
                console.error('Помилка визначення адреси:', error);
                showNotification('Неможливо визначити адресу!', 'danger');
            });
    });

    document.getElementById('searchAddressBtn').addEventListener('click', searchByAddress);

    document.getElementById('searchAddress').addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            searchByAddress();
        }
    });

    document.getElementById('findNearbyBtn').addEventListener('click', findNearby);
}

function searchByAddress() {
    const address = document.getElementById('searchAddress').value;

    if (!address) {
        showNotification('Введіть адресу для пошуку!', 'warning');
        return;
    }

    fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(address)}&limit=1`)
        .then(response => response.json())
        .then(data => {
            if (data && data.length > 0) {
                const location = {
                    lat: parseFloat(data[0].lat),
                    lng: parseFloat(data[0].lon)
                };

                map.setView([location.lat, location.lng], 15);

                const tempMarker = L.marker([location.lat, location.lng], {
                    title: address,
                    icon: L.divIcon({
                        className: 'custom-marker',
                        html: `<div style="background-color: #3498db; width: 24px; height: 24px; border-radius: 50%; border: 2px solid white;"></div>`,
                        iconSize: [24, 24],
                        iconAnchor: [12, 12]
                    })
                }).addTo(map);

                setTimeout(() => {
                    map.removeLayer(tempMarker);
                }, 5000);
            } else {
                showNotification('Адресу не знайдено!', 'warning');
            }
        })
        .catch(error => {
            console.error('Помилка пошуку адреси:', error);
            showNotification('Помилка пошуку адреси!', 'danger');
        });
}

function findNearby() {
    if (navigator.geolocation) {
        const options = {
            enableHighAccuracy: true,
            timeout: 15000,
            maximumAge: 0
        };

        showNotification('Визначаємо ваше місцезнаходження...', 'info');

        navigator.geolocation.getCurrentPosition(function(position) {
            const pos = {
                lat: position.coords.latitude,
                lng: position.coords.longitude
            };

            currentPosition = pos;

            map.setView([pos.lat, pos.lng], 17);

            const accuracy = position.coords.accuracy;

            const userMarker = L.marker([pos.lat, pos.lng], {
                title: `Ваше місцезнаходження (точність: ${Math.round(accuracy)} м)`,
                icon: L.divIcon({
                    className: 'custom-marker',
                    html: `<div style="background-color: #3498db; width: 24px; height: 24px; border-radius: 50%; border: 2px solid white;"></div>`,
                    iconSize: [24, 24],
                    iconAnchor: [12, 12]
                })
            }).addTo(map);

            const accuracyCircle = L.circle([pos.lat, pos.lng], {
                radius: accuracy,
                color: '#3498db',
                fillColor: '#3498db',
                fillOpacity: 0.15,
                weight: 1
            }).addTo(map);

            const searchCircle = L.circle([pos.lat, pos.lng], {
                radius: 3000,
                color: '#28a745',
                fillColor: '#28a745',
                fillOpacity: 0.05,
                weight: 1,
                dashArray: '5, 5'
            }).addTo(map);

            window.userLocationMarker = userMarker;
            window.userAccuracyCircle = accuracyCircle;
            window.userSearchCircle = searchCircle;

            showNotification(`Ваше місцезнаходження визначено з точністю ${Math.round(accuracy)} метрів`, 'success');

            setTimeout(() => {
                if (window.userLocationMarker) map.removeLayer(window.userLocationMarker);
                if (window.userAccuracyCircle) map.removeLayer(window.userAccuracyCircle);
                if (window.userSearchCircle) map.removeLayer(window.userSearchCircle);
            }, 30000); 

            findNearbyMarkers(pos);
        }, function(error) {
            console.error("Помилка геолокації:", error);
            let errorMessage = 'Неможливо визначити ваше місцезнаходження!';

            switch(error.code) {
                case error.PERMISSION_DENIED:
                    errorMessage = 'Ви відмовили у доступі до геолокації. Перевірте налаштування браузера.';
                    break;
                case error.POSITION_UNAVAILABLE:
                    errorMessage = 'Інформація про місцезнаходження недоступна. Перевірте GPS або мережу.';
                    break;
                case error.TIMEOUT:
                    errorMessage = 'Час очікування для визначення місцезнаходження вичерпано. Спробуйте ще раз.';
                    break;
            }

            showNotification(errorMessage, 'danger');
        }, options);
    } else {
        showNotification('Ваш браузер не підтримує геолокацію!', 'danger');
    }
}

function getDirectionsUrl(lat, lng) {
    if (currentPosition) {
        return `https://www.openstreetmap.org/directions?engine=fossgis_osrm_car&route=${currentPosition.lat},${currentPosition.lng};${lat},${lng}`;
    } else {
        return `https://www.openstreetmap.org/?mlat=${lat}&mlon=${lng}#map=16/${lat}/${lng}`;
    }
}

function copyToClipboard(text) {
    if (!text) {
        showNotification('Немає даних для копіювання!', 'warning');
        return;
    }

    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(text)
            .then(() => {
                showNotification('Координати скопійовано!', 'success');
            })
            .catch(err => {
                console.error('Помилка копіювання: ', err);
                showNotification('Помилка при копіюванні!', 'danger');
                fallbackCopyTextToClipboard(text);
            });
    } else {
        fallbackCopyTextToClipboard(text);
    }
}

function fallbackCopyTextToClipboard(text) {
    const textArea = document.createElement("textarea");
    textArea.value = text;

    textArea.style.position = "fixed";
    textArea.style.top = 0;
    textArea.style.left = 0;
    textArea.style.width = "2em";
    textArea.style.height = "2em";
    textArea.style.padding = 0;
    textArea.style.border = "none";
    textArea.style.outline = "none";
    textArea.style.boxShadow = "none";
    textArea.style.background = "transparent";

    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();

    try {
        const successful = document.execCommand('copy');
        if (successful) {
            showNotification('Координати скопійовано!', 'success');
        } else {
            showNotification('Помилка при копіюванні!', 'danger');
        }
    } catch (err) {
        console.error('Помилка копіювання: ', err);
        showNotification('Помилка при копіюванні!', 'danger');
    }

    document.body.removeChild(textArea);
}

function findNearbyMarkers(position, radius = 3) {
    const nearbyMarkers = markers.filter(marker => {
        const distance = calculateDistance(
            position.lat, position.lng,
            marker.latitude, marker.longitude
        );
        return distance <= radius;
    });
}

function calculateDistance(lat1, lon1, lat2, lon2) {
    const R = 6371;
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a = Math.sin(dLat/2) * Math.sin(dLat/2) +
        Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
        Math.sin(dLon/2) * Math.sin(dLon/2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
    const distance = R * c;
    return distance;
}

function showNotification(message, type) {
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} notification-toast`;
    notification.innerHTML = message;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.classList.add('show');
    }, 100);

    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => {
            document.body.removeChild(notification);
        }, 500);
    }, 5000);
}

function formatDate(date) {
    const options = { year: 'numeric', month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' };
    return new Date(date).toLocaleDateString('uk-UA', options);
}

function renderMarkerStatusIcons(marker) {
    let html = '';

    if (marker.hasRamp) {
        html += '<i class="fas fa-wheelchair text-success me-1" title="Є пандус"></i>';
    }
    if (marker.hasBlindSupport) {
        html += '<i class="fas fa-eye-slash text-success me-1" title="Є обладнання для незрячих"></i>';
    }
    if (marker.hasElevator) {
        html += '<i class="fas fa-arrow-alt-circle-up text-success me-1" title="Є ліфт"></i>';
    }
    if (marker.hasAccessibleToilet) {
        html += '<i class="fas fa-restroom text-success me-1" title="Є доступний туалет"></i>';
    }
    if (marker.hasParking) {
        html += '<i class="fas fa-parking text-success me-1" title="Є паркування для інвалідів"></i>';
    }

    return html;
}

document.addEventListener('DOMContentLoaded', function() {
    const style = document.createElement('style');
    style.textContent = `
        .notification-toast {
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            min-width: 250px;
            max-width: 400px;
            opacity: 0;
            transform: translateY(-20px);
            transition: all 0.5s ease;
        }
        .notification-toast.show {
            opacity: 1;
            transform: translateY(0);
        }
        .map-container {
            position: relative;
            height: 75vh;
            border-radius: 8px;
            overflow: hidden;
        }
        #accessibilityMap {
            height: 100%;
            width: 100%;
        }
        #addMarkerHelp {
            position: absolute;
            top: 10px;
            left: 50%;
            transform: translateX(-50%);
            z-index: 1000;
            width: 80%;
        }
        .marker-icon {
            width: 16px;
            height: 16px;
            border-radius: 50%;
        }
        .user-marker-item {
            padding: 10px;
            border-bottom: 1px solid #eee;
            cursor: pointer;
            transition: background-color 0.2s ease;
        }
        .user-marker-item:hover {
            background-color: #f8f9fa;
        }
        .feature-available {
            color: #28a745;
            font-weight: 500;
        }
        .feature-unavailable {
            color: #6c757d;
        }
        .custom-marker {
            display: flex;
            justify-content: center;
            align-items: center;
        }
    `;
    document.head.appendChild(style);
});

document.addEventListener('DOMContentLoaded', initMap);