from pathlib import Path
import json,shutil,re
P=Path('C:/Projects/CodeAssist');D=P/'docs/validation/ui-comparison-0.0.88-20261018';R=P/'.local/parity-runs/20261018-105950-683'
for n in ['CA.results.json','CA.plan.json','localization-retry.json']:shutil.copy2(R/n,D/n)
p=P/'docs/ui-comparison-0.0.88.html';s=p.read_text(encoding='utf-8');s=s.replace('<h2>계산 기준</h2>','<h2>화면에서 확인한 차이와 미포함 범위</h2><p>Display: VA의 정렬 방식·내비게이션 위치·열 표시·Quick Info 세부 토글이 CA 화면에는 없습니다. 프레임 크기와 제목 시작 위치는 비슷하지만 항목 수와 배치는 다릅니다.</p><p>이 갤러리는 기존 108개 시나리오 기준입니다. Welcome 전체 페이지, 모든 툴바·드롭다운·자동완성 상태, 모든 라이트 테마 상태는 포괄 촬영되지 않았습니다. 66개는 고유 페이지·명령 분류이며 제품 전체 화면 수가 아닙니다. 전체 UI 유사도는 이 점수만으로 확정할 수 없습니다.</p><h2>계산 기준</h2>');s=s.replace('</html>','<p>작업 시작 2026-10-18 10:58:22 KST · 제안 시간 8–10분 · XERJ 사용: 검색 누락은 rg와 현재 소스로 보완.</p></html>');p.write_text(s,encoding='utf-8')
p=P/'docs/layout-0.0.88.html';s=p.read_text(encoding='utf-8');s=s.replace('</html>','<p><a href="ui-comparison-0.0.88.html">최신 VA·CA 108개 시나리오 실제 UI 대조 화면과 계산 결과</a></p></html>');p.write_text(s,encoding='utf-8')
print('Images:',len(list(D.glob('*.png'))));print('Rows:',len(json.loads((D/'summary.json').read_text(encoding='utf-8'))['rows']))
s=(P/'docs/ui-comparison-0.0.88.html').read_text(encoding='utf-8');print('Missing images:',[x for x in re.findall(r'src="([^"]+)"',s) if not (P/'docs'/x).exists()])
