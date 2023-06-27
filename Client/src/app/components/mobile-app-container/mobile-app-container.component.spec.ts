import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MobileAppContainerComponent } from './mobile-app-container.component';

describe('MobileAppContainerComponent', () => {
  let component: MobileAppContainerComponent;
  let fixture: ComponentFixture<MobileAppContainerComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [MobileAppContainerComponent]
    });
    fixture = TestBed.createComponent(MobileAppContainerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
