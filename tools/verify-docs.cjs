const fs=require('node:fs');
const path=require('node:path');
const {pathToFileURL}=require('node:url');
const {chromium}=require('playwright');
(async()=>{
  const root=path.resolve(__dirname,'..','docs');
  const files=['localization.html','progress-audit.html','interface.html','startup.html','handoff.html','integrated-spec.html','index.html','usage.html','keyboard-shortcuts.html','architecture.html','analysis-integration.html','validation/index.html','reverse-engineering/va-2630/index.html'];
  const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  try{
    const page=await browser.newPage({viewport:{width:1440,height:1000}});
    const errors=[];page.on('pageerror',error=>errors.push(error.message));
    let checked=0;
    for(const file of files){
      await page.goto(pathToFileURL(path.join(root,file)).href);
      await page.locator('article h1').waitFor();
      const links=await page.locator('a[href],link[href],script[src]').evaluateAll(nodes=>nodes.map(node=>node.getAttribute('href')||node.getAttribute('src')));
      for(const link of links){
        if(!link||link.startsWith('#')||/^[a-z]+:/i.test(link))continue;
        const local=path.resolve(path.dirname(path.join(root,file)),decodeURIComponent(link.split('#')[0]));
        if(!fs.existsSync(local))throw new Error(`${file}: broken link ${link}`);
        checked++;
      }
    }
    await page.goto(pathToFileURL(path.join(root,'index.html')).href);
    await page.locator('#doc-search').fill('단축키');
    if(await page.locator('nav a:not([hidden])').count()===0)throw new Error('Search did not return results');
    await page.locator('#doc-search').fill('');
    await page.locator('#theme-toggle').click();
    if(await page.locator('body').getAttribute('data-theme')!=='dark')throw new Error('Theme toggle failed');
    await page.locator('#theme-toggle').click();
    const artifacts=path.resolve(__dirname,'obj');fs.mkdirSync(artifacts,{recursive:true});
    await page.screenshot({path:path.join(artifacts,'docs-desktop.png'),fullPage:true});
    await page.setViewportSize({width:390,height:844});
    await page.goto(pathToFileURL(path.join(root,'keyboard-shortcuts.html')).href);
    const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>window.innerWidth);
    if(overflow)throw new Error('Mobile page overflows horizontally');
    await page.screenshot({path:path.join(artifacts,'docs-mobile.png'),fullPage:true});
    if(errors.length)throw new Error(errors.join('\n'));
    console.log(`Verified ${files.length} HTML pages, ${checked} local links, search, theme and mobile width.`);
  }finally{await browser.close()}
})().catch(error=>{console.error(error);process.exitCode=1});
