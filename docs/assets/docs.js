document.querySelectorAll('table').forEach(table=>{const wrapper=document.createElement('div');wrapper.className='table-scroll';table.before(wrapper);wrapper.append(table)});
const search=document.getElementById('doc-search');
search.addEventListener('input',()=>{const query=search.value.trim().toLocaleLowerCase();let count=0;document.querySelectorAll('nav a').forEach(link=>{link.hidden=query!==''&&!link.dataset.search.toLocaleLowerCase().includes(query);if(!link.hidden)count++});document.getElementById('search-status').textContent=query?`${count}개 문서에서 일치`:''});
try{if(localStorage.getItem('codeassist-docs-theme')==='dark')document.body.dataset.theme='dark'}catch{}
document.getElementById('theme-toggle').addEventListener('click',()=>{const theme=document.body.dataset.theme==='dark'?'light':'dark';document.body.dataset.theme=theme;try{localStorage.setItem('codeassist-docs-theme',theme)}catch{}});
