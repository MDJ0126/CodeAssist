const fs = require('node:fs');
const path = require('node:path');
const { marked } = require('marked');

const root = path.resolve(__dirname, '..', 'docs');
const manifest = fs.readFileSync(path.resolve(__dirname, '..', 'CodeAssist.Extension', 'source.extension.vsixmanifest'), 'utf8');
const versionMatch = manifest.match(/<Identity\b[^>]*\bVersion="([^"]+)"/);
if (!versionMatch) throw new Error('VSIX Identity version missing');
const version = versionMatch[1];
const pages = [
  ['README.md', '문서 홈'], ['remaining-features.md', '남은 70개 항목'], ['interface.md', 'CodeAssist 인터페이스'], ['progress-audit.md', '동기화율 · 구현율 검증'], ['localization.md', '언어 설정 · 번역표'], ['startup.md', '첫 실행 · Welcome'], ['handoff.md', '작업 인계'], ['usage.md', '사용 안내'], ['keyboard-shortcuts.md', '단축키'],
  ['architecture.md', '구조와 캐시'], ['integrated-spec.md', '통합 분석 명세'], ['analysis-integration.md', '분석 적용 현황'],
  ['validation/README.md', '검증 결과'], ['reverse-engineering/va-2630/README.md', '역분석 기록'],
];
const output = name => name.replace(/(?:^|\/)README\.md$/, match => match.startsWith('/') ? '/index.html' : 'index.html').replace(/\.md$/, '.html');
const escape = value => value.replace(/[&<>"']/g, ch => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[ch]));
const relative = (from, to) => path.relative(path.dirname(from), to).split(path.sep).join('/') || 'index.html';
const source = pages.map(([name, title]) => ({name, title, text:fs.readFileSync(path.join(root, name), 'utf8')}));

for (const page of source) {
  const destination = path.join(root, output(page.name));
  const nav = source.map(item => `<a href="${escape(relative(destination, path.join(root, output(item.name))))}" ${item.name === page.name ? 'aria-current="page"' : ''} data-search="${escape(item.title + ' ' + item.text.replace(/\s+/g, ' '))}">${escape(item.title)}</a>`).join('\n');
  const body = marked.parse(page.text, {gfm:true}).replace(/href="([^"#]+\.md)(#[^"]*)?"/g, (full, url, fragment = '') => {
    if (/^[a-z]+:/i.test(url)) return full;
    return `href="${escape(output(url))}${fragment}"`;
  });
  const css = escape(relative(destination, path.join(root, 'assets/docs.css')));
  const js = escape(relative(destination, path.join(root, 'assets/docs.js')));
  const home = escape(relative(destination, path.join(root, 'index.html')));
  const html = `<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<meta name="description" content="CodeAssist ${escape(version)} — ${escape(page.title)}"><link rel="icon" type="image/png" href="${escape(relative(destination, path.join(root, 'assets/favicon.png')))}"><title>${escape(page.title)} · CodeAssist</title>
<link rel="stylesheet" href="${css}"><script defer src="${js}"></script></head>
<body><a class="skip" href="#content">본문으로 이동</a>
<aside class="sidebar"><a class="brand" href="${home}"><span class="logo">CA</span><span>CodeAssist<small>DOCUMENTATION</small></span></a>
<div class="version">Visual Studio 2022 <span>v${escape(version)}</span></div>
<label for="doc-search">문서 검색</label><input id="doc-search" type="search" placeholder="키워드를 입력하세요" autocomplete="off">
<p id="search-status" aria-live="polite"></p><nav aria-label="문서 목차">${nav}</nav>
<div class="sidebar-note">범용 개발 보조 도구<br>현재 C++ · Unreal 중심</div></aside>
<div class="workspace"><header><span>CodeAssist / ${escape(page.title)}</span><button id="theme-toggle" type="button" aria-label="밝은 테마와 어두운 테마 전환">테마 전환</button></header>
<main id="content" tabindex="-1"><div class="eyebrow">CODEASSIST DOCUMENTATION</div><article>${body}</article>
<footer>CodeAssist ${escape(version)} · 확인된 구현과 미확인 사항을 구분해 기록합니다.</footer></main></div></body></html>`;
  fs.writeFileSync(destination, html);
}
console.log(`Generated ${source.length} offline HTML documentation pages.`);
