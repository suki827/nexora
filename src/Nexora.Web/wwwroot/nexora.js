let csrfToken;

async function token() {
    if (csrfToken) return csrfToken;
    const response = await fetch('/api/auth/csrf', { credentials: 'same-origin' });
    if (!response.ok) throw new Error('Could not get the security token.');
    csrfToken = (await response.json()).token;
    return csrfToken;
}

async function request(method, path, body, binary = false) {
    const headers = {};
    if (method !== 'GET') headers['X-CSRF-TOKEN'] = await token();
    if (body != null && !binary) headers['Content-Type'] = 'application/json';
    const response = await fetch(path, {
        method, credentials: 'same-origin', headers,
        body: body == null ? undefined : binary ? body : JSON.stringify(body)
    });
    if (response.status === 401) {
        if (path !== '/api/auth/login') window.location.assign('/login');
        throw new Error('Please sign in first.');
    }
    if (!response.ok) {
        const message = await response.text();
        throw new Error(`${response.status}: ${message.slice(0, 400)}`);
    }
    if (method === 'POST' && (path === '/api/auth/login' || path === '/api/auth/register')) csrfToken = undefined;
    return response.status === 204 ? null : await response.json();
}

export function get(path) { return request('GET', path); }
export function send(method, path, body) { return request(method, path, body); }

export async function uploadFile(assetId, inputId, callback) {
    const file = document.getElementById(inputId)?.files?.[0];
    if (!file) throw new Error('Choose a file first.');
    if (file.size < 1 || file.size > 20 * 1024 * 1024 * 1024) throw new Error('File size must be between 1 byte and 20 GiB.');
    const chunkSize = 8 * 1024 * 1024;
    const base = `/api/assets/${encodeURIComponent(assetId)}/uploads`;
    const resumeKey = `nexora.upload.${assetId}.${file.name}.${file.size}.${file.lastModified}`;
    let session;
    const savedId = localStorage.getItem(resumeKey);
    if (savedId) {
        try { session = await request('GET', `${base}/${encodeURIComponent(savedId)}`); }
        catch { localStorage.removeItem(resumeKey); }
    }
    if (!session || session.status !== 'uploading') {
        session = await request('POST', base, {
            fileRole: 'original', originalName: file.name, contentType: file.type || null,
            totalBytes: file.size, chunkSize
        });
        localStorage.setItem(resumeKey, session.fileId);
    }
    const target = `${base}/${encodeURIComponent(session.fileId)}`;
    const chunkCount = Math.ceil(file.size / chunkSize);
    const completed = new Set(session.uploadedChunks ?? []);
    await callback.invokeMethodAsync('Report', Math.round(completed.size / chunkCount * 100));
    for (let index = 0; index < chunkCount; index++) {
        if (completed.has(index)) continue;
        const blob = file.slice(index * chunkSize, Math.min((index + 1) * chunkSize, file.size));
        await request('PUT', `${target}/chunks/${index}`, blob, true);
        await callback.invokeMethodAsync('Report', Math.round((index + 1) / chunkCount * 100));
    }
    await request('POST', `${target}/complete`);
    localStorage.removeItem(resumeKey);
}

export async function drawWaveform(assetId, fileId) {
    const data = await request('GET', `/api/assets/${encodeURIComponent(assetId)}/files/${encodeURIComponent(fileId)}/waveform`);
    const container = document.getElementById('waveform-container');
    if (!container) return;
    container.replaceChildren();
    const canvas = document.createElement('canvas');
    const width = Math.max(300, container.clientWidth);
    const height = 160;
    const ratio = window.devicePixelRatio || 1;
    canvas.width = width * ratio;
    canvas.height = height * ratio;
    canvas.style.width = '100%';
    canvas.style.height = `${height}px`;
    container.appendChild(canvas);
    const ctx = canvas.getContext('2d');
    ctx.scale(ratio, ratio);
    ctx.fillStyle = '#f4f8fc';
    ctx.fillRect(0, 0, width, height);
    ctx.strokeStyle = '#2b9d91';
    ctx.lineWidth = 1;
    const peaks = data.peaks || [];
    for (let x = 0; x < width && peaks.length; x++) {
        const start = Math.floor(x * peaks.length / width);
        const end = Math.max(start + 1, Math.ceil((x + 1) * peaks.length / width));
        let min = 0, max = 0;
        for (let i = start; i < Math.min(end, peaks.length); i++) {
            min = Math.min(min, peaks[i][0]);
            max = Math.max(max, peaks[i][1]);
        }
        ctx.beginPath();
        ctx.moveTo(x + .5, (1 - max) * height / 2);
        ctx.lineTo(x + .5, (1 - min) * height / 2);
        ctx.stroke();
    }
}
