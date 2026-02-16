console.log("gallery.js loaded");

/* =========================
   Работа с title окна
========================= */

/**
 * Укорачивает длинное имя файла
 */
function shortenFilename(name, max = 40) {
    return name.length > max
        ? name.slice(0, max - 1) + "…"
        : name;
}

/**
 * Устанавливает title приложения:
 * "12 / 250     filename.jpg"
 */
function setAppTitle(index, total, filename) {
    const spacer = "\u00A0\u00A0\u00A0\u00A0\u00A0"; // 5 неразрывных пробелов
    document.title = `${index} / ${total}${spacer}${shortenFilename(filename)}`;
}

/**
 * Сбрасывает title к базовому имени приложения
 */
const DEFAULT_TITLE = "Gallery";

// реагирует на выход из гелереи, для изменения названия картинки в шапке окна
let switchingFromGallery = false;

function resetTitle() {
    if (switchingFromGallery) return;
    document.title = DEFAULT_TITLE;
}

/* =========================
   API
========================= */

/**
 * Загружает список файлов с сервера
 * window.urlServer - базовый URL сервера, установлен в images.js, например: "http://localhost:5000"
 */
async function fetchFileList(path) {
    return fetch(
        window.urlServer + "/api/files?path=" + encodeURIComponent(path)
    ).then(r => r.json());
}

/**
 * Загружает один файл как Blob
 */
// async function fetchImageBlob(path, name) {
//     return fetch(
//         window.urlServer + "/api/file?path=" +
//         encodeURIComponent(path) +
//         "&name=" + encodeURIComponent(name)
//     ).then(r => r.blob());
// }
async function fetchImageBlobByUrl(url, signal) {
    const resp = await fetch(url, { signal });
    if (!resp.ok) throw new Error(`HTTP ${resp.status} for ${url}`);
    return await resp.blob();
}

/* =========================
   Работа с изображениями
========================= */

/**
 * Получает оригинальные размеры изображения
 */
async function getImageSize(blob) {
    const img = new Image();

    return new Promise(resolve => {
        img.onload = function () {
            resolve({ width: this.width, height: this.height });
            URL.revokeObjectURL(this.src);
        };
        img.src = URL.createObjectURL(blob);
    });
}

/**
 * Создаёт bitmap-превью с сохранением пропорций
 */
async function createPreviewBitmap(blob, size, original) {
    const ratio = original.width / original.height;

    let resizeWidth, resizeHeight;

    if (ratio >= 1) {
        resizeWidth = size;
        resizeHeight = size / ratio;
    } else {
        resizeHeight = size;
        resizeWidth = size * ratio;
    }

    return createImageBitmap(blob, {
        resizeWidth: Math.round(resizeWidth),
        resizeHeight: Math.round(resizeHeight),
        resizeQuality: "low"
    });
}

/**
 * Рисует bitmap по центру canvas
 */
function drawCentered(canvas, bmp) {
    const ctx = canvas.getContext("2d");
    const dx = (canvas.width - bmp.width) / 2;
    const dy = (canvas.height - bmp.height) / 2;
    ctx.drawImage(bmp, dx, dy);
}


/* =========================
   UI
========================= */

/**
 * Создаёт canvas-элементы галереи и
 * вешает hover для изменения title
 */
function createGalleryCanvases(container, files, size = 150) {
    container.innerHTML = "";

    const canvases = [];

    files.forEach((name, index) => {
        const c = document.createElement("canvas");
        c.width = size;
        c.height = size;

        const ctx = c.getContext("2d");
        ctx.fillStyle = "#fff"; // цвет фона превью картинки
        ctx.fillRect(0, 0, size, size);

        /* hover title */
        c.addEventListener("mouseenter", () => {
            setAppTitle(index + 1, files.length, name);
        });
        c.addEventListener("mouseleave", resetTitle);

        /* 🔥 КЛИК → открыть viewer */
        c.addEventListener("click", () => {
            openImageFromGallery(index);
        });

        canvases.push(c);
        container.appendChild(c);
    });

    return canvases;
}



/* =========================
   Воркеры
========================= */

/**
 * Рассчитывает оптимальное количество параллельных воркеров
 */
function calculateWorkers(filesCount) {
    //const cpuCores = navigator.hardwareConcurrency || 4;
    //const optimalForHeavy = Math.max(2, Math.floor(cpuCores * 0.75));
    //const maxWorkers = Math.min(optimalForHeavy, 12);

    //return Math.min(maxWorkers, filesCount);
    // к сожалению, много воркеров на больших файлах (>5МБ) приводит к подвисаниям UI)
    return 2;
}

/* =========================
   Основная логика
========================= */

// контроллер отмены текущей загрузки
let currentLoadController = null;

/**
 * Основная функция загрузки и отображения галереи
 */
async function load() {
    // ⛔ отменяем предыдущую загрузку
    if (currentLoadController) {
        currentLoadController.abort();
    }

    const controller = new AbortController();
    const signal = controller.signal;
    currentLoadController = controller;

    const gallery = document.getElementById("gallery");


    const items = window.images; // [{Name, Path}, ...]
    const files = items.map(x => x.Name);
    const urls  = items.map(x => x.Path);

    if (signal.aborted) return;

    const canvases = createGalleryCanvases(gallery, files);
    const queue = files.map((name, index) => ({ name, index }));
    const workersCount = calculateWorkers(files.length);

    let queueIndex = 0;
    let processed = 0;

    async function worker() {
    while (true) {
        if (signal.aborted) return;

        const i = queueIndex++;
        if (i >= files.length) return;

        const url = urls[i];

        let blob;
        try {
            blob = await fetchImageBlobByUrl(url, signal);
        } catch (e) {
            if (!signal.aborted) console.error("Preview load failed:", url, e);
            return;
        }

        if (signal.aborted) return;

        const original = await getImageSize(blob);
        if (signal.aborted) return;

        const bmp = await createPreviewBitmap(blob, 150, original);
        if (signal.aborted) { bmp.close(); return; }

        drawCentered(canvases[i], bmp);
        bmp.close();

        processed++;
        if (processed % 4 === 0) await new Promise(r => requestAnimationFrame(r));
    }
}


    await Promise.all(
        Array.from({ length: workersCount }, worker)
    );

    // ✅ гарантированно выполнится только если это актуальный load
    if (!signal.aborted) {
        resetTitle();
    }
}


