import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BarogramComponent } from './barogram.component';

describe('BarogramComponent', () => {
  let component: BarogramComponent;
  let fixture: ComponentFixture<BarogramComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [BarogramComponent]
    });
    fixture = TestBed.createComponent(BarogramComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
