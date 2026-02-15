console.log("switch.js loaded");

/* === SHOW / HIDE === */

let galleryLoaded = false; // флаг, указывающий, загружена ли галерея
let galleryLoading = false; // защита от повторной загрузки, если пользователь быстро открывает и закрывает галерею
function showGallery() {
    gallery.classList.remove('hidden');

    if (galleryLoaded || galleryLoading) return;

    galleryLoading = true;
    load().then(() => {
        galleryLoaded = true;
        galleryLoading = false;
    });
}
//function showGallery() {
//    gallery.classList.remove('hidden');

//    load();
//}

function hideGallery() {
    gallery.classList.add('hidden');
}

function toggleGallery() {
    gallery.classList.contains('hidden')
        ? showGallery()
        : hideGallery();
}

/* === HOTKEY Ctrl+X / Ctrl+Ч === */
document.addEventListener('keydown', e => {
    if (!e.ctrlKey) return;
    if (e.code === 'KeyX' || e.code === 'KeyЧ') {
        e.preventDefault();
        toggleGallery();
    }
});

/* === OPEN IMAGE FROM GALLERY === */

function openImageFromGallery(targetIndex) {
    // в resetTitle() gallery.js не разрешает менять title при уходе мыши с картинки
    // иначе стандартое поведение переписывает название файла на "Gallery"
    switchingFromGallery = true;

    // обновляем индекс
    index = targetIndex;

    // title теперь будет жить в viewer
    document.title = images[index].Name;

    // скрываем галерею
    hideGallery();

    // сразу показываем нужную картинку
    show();

    // после завершения перехода возвращаем нормальное поведение
    requestAnimationFrame(() => {
        switchingFromGallery = false;
    });
}

/* === RELOAD GALLERY === */
// пока нигде не используется, но может пригодиться
function reloadGallery() {
    galleryLoaded = false;
    galleryLoading = false;
    showGallery();
}